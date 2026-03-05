using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input.ForceFeedback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WinUiTemplate.Services.Interfaces;
using Xunit;

namespace WinUiTemplate.Services
{
    public class ArchiveService : IArchiveService
    {
        // Services & Stores
        private readonly IFileUtils fileUtils;
        private readonly ILoggerService logger;

        // Members
        private const int bufferSize = 81920;

        // Constructors

        public ArchiveService(IServiceProvider serviceProvider) {
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Events
        public event Action<ZipProgress>? ProgressChanged;

        // Public Functions

        public async Task<OperationResult> ZipFolderAsync(string sourceFolder, string zipFilePath, CancellationToken cancellationToken = default) {
            FileResult zipFileResult = await fileUtils.TryGetFileAsync(zipFilePath);
            if (zipFileResult.Success && zipFileResult.File != null) {
                await zipFileResult.File.DeleteAsync();
            }

            FolderResult sourceFolderResult = await fileUtils.TryGetOrCreateFolderAsync(sourceFolder);
            if (!sourceFolderResult.Success || sourceFolderResult.Folder == null) {
                return new OperationResult(false, $"Failed to access sourceFolder: '{sourceFolder}'", true);
            }

            StorageFile? zipFile = null;
            string? parent = Path.GetDirectoryName(zipFilePath);
            if (parent != null) {
                FolderResult parentResult = await fileUtils.TryGetOrCreateFolderAsync(parent);
                if (!parentResult.Success || parentResult.Folder == null) {
                    return new OperationResult(false, "Failed to create zip parent directory", true);
                }

                zipFile = await parentResult.Folder.CreateFileAsync(Path.GetFileName(zipFilePath));
            }

            if (zipFile == null) return new OperationResult(false, "Failed to create zip file", true);

            try {
                FilesResult filesResult = await fileUtils.TryGetAllFilesAsync(sourceFolder, cancellationToken);
                if (!filesResult.Success || filesResult.Files == null) return new OperationResult(false, "Failed to get files in source folder", false);

                ulong totalBytes = 0;
                ulong processedBytes = 0;

                foreach(StorageFile file in filesResult.Files) {
                    totalBytes += (await file.GetBasicPropertiesAsync()).Size;
                }

                using (Stream zipStream = await zipFile.OpenStreamForWriteAsync()) {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create, false, Encoding.UTF8)) {
                        foreach(StorageFile file in filesResult.Files) {
                            cancellationToken.ThrowIfCancellationRequested();

                            string relativePath = fileUtils.GetRelativePath(sourceFolder, file.Path);
                            ZipArchiveEntry entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);

                            using (Stream entryStream = entry.Open()) {
                                using (Stream fileStream = await file.OpenStreamForReadAsync()) {
                                    byte[] buffer = new byte[bufferSize];
                                    int read = 0;
                                    while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0) {
                                        await entryStream.WriteAsync(buffer, 0, read, cancellationToken);
                                        processedBytes += (ulong)read;
                                        double percent = totalBytes == 0 ? 0 : (double)processedBytes / totalBytes;
                                        ProgressChanged?.Invoke(new ZipProgress(percent, "Creating backup"));
                                    }
                                }
                            }
                        }
                    }
                }

                return new OperationResult(true, "", false);
            }
            catch (OperationCanceledException) {
                return new OperationResult(false, "Backup cancelled", false);
            }
            catch (Exception e) {
                string error = $"Archive failed - {e.Message}";
                logger.LogError(error);
                return new OperationResult(false, error, true);
            }
        }

        public async Task<OperationResult> ExtractZip(string zipPath, string destinationFolder, CancellationToken cancellationToken = default) {
            FileResult zipResult = await fileUtils.TryGetFileAsync(zipPath);
            if(!zipResult.Success || zipResult.File == null) {
                return new OperationResult(false, $"Failed to access zip file '{zipPath}'", true);
            }

            FolderResult destinationResult = await fileUtils.TryGetOrCreateFolderAsync(destinationFolder);
            if (!destinationResult.Success || destinationResult.Folder == null) {
                return new OperationResult(false, $"Failed to create or get destination folder '{destinationFolder}'", true);
            }

            try {
                using (Stream zipStream = await zipResult.File.OpenStreamForReadAsync()) {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, false, Encoding.UTF8)) {
                        foreach(ZipArchiveEntry entry in archive.Entries) {
                            cancellationToken.ThrowIfCancellationRequested();

                            string destinationPath = Path.Combine(destinationFolder, entry.FullName);
                            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                            if (destinationDirectory == null) return new OperationResult(false, $"Invalid parent directory of file: '{entry.FullName}'", true);

                            FolderResult destinationDirectoryResult = await fileUtils.TryGetOrCreateFolderAsync(destinationDirectory);
                            if(!destinationDirectoryResult.Success || destinationDirectoryResult.Folder == null) {
                                return new OperationResult(false, $"Failed to get or create destination folder: '{destinationDirectory}'", true);
                            }

                            using (Stream entryStream = entry.Open()) {
                                using (FileStream fileStream = new FileStream(
                                    destinationPath,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.None,
                                    bufferSize,
                                    true
                                )) {
                                    await entryStream.CopyToAsync(fileStream, bufferSize, cancellationToken);
                                }
                            }
                        }
                    }
                }

                return new OperationResult(true, "", false);
            }
            catch (OperationCanceledException) {
                return new OperationResult(false, "Restore cancelled", false);
            }
            catch (Exception e) {
                string error = $"Unzip failed: {e.Message}";
                logger.LogError(error);
                return new OperationResult(false, error, true);
            }
        }
    }

    #region Unit Tests

    public class ArchiveServiceTests 
    {
        private readonly Mock<IFileUtils> _mockFileUtils;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ArchiveService _archiveService;

        public ArchiveServiceTests() {
            _mockFileUtils = new Mock<IFileUtils>();
            _mockLogger = new Mock<ILoggerService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(_mockFileUtils.Object);
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(_mockLogger.Object);

            _archiveService = new ArchiveService(_mockServiceProvider.Object);
        }

        [Fact]
        public async Task Test_ZipFolderAsync_SourceFolderDoesNotExist() {
            var sourceFolder = @"C:\NonExistent";
            var zipPath = @"C:\output.zip";

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null, null));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(sourceFolder))
                .ReturnsAsync(new FolderResult(false, null, null));

            // Act
            var result = await _archiveService.ZipFolderAsync(sourceFolder, zipPath);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to access sourceFolder");
        }
    }

    #endregion
}
