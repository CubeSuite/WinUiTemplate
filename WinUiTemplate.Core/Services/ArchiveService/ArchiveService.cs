using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Core.Services.Interfaces;

namespace WinUiTemplate.Core.Services
{
    public class ArchiveService : IArchiveService
    {
        // Services & Stores
        private readonly IFileUtils fileUtils;
        private readonly ILoggerService logger;

        // Fields
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
            FolderResult sourceFolderResult = await fileUtils.TryGetOrCreateFolderAsync(sourceFolder);
            if (!sourceFolderResult.Success || sourceFolderResult.Folder == null) {
                return new OperationResult(false, $"Failed to access sourceFolder: '{sourceFolder}'", true);
            }

            string tempZipFilePath = zipFilePath + ".tmp";
            StorageFile? zipFile = null;
            string? parent = Path.GetDirectoryName(zipFilePath);
            if (parent != null) {
                FolderResult parentResult = await fileUtils.TryGetOrCreateFolderAsync(parent);
                if (!parentResult.Success || parentResult.Folder == null) {
                    return new OperationResult(false, "Failed to create zip parent directory", true);
                }

                zipFile = await parentResult.Folder.CreateFileAsync(Path.GetFileName(tempZipFilePath), CreationCollisionOption.ReplaceExisting);
            }

            if (zipFile == null) return new OperationResult(false, "Failed to create zip file", true);

            try {
                FilesResult filesResult = await fileUtils.TryGetAllFilesAsync(sourceFolder, cancellationToken);
                if (!filesResult.Success || filesResult.Files == null) return new OperationResult(false, "Failed to get files in source folder", true);

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

                await zipFile.RenameAsync(Path.GetFileName(zipFilePath), NameCollisionOption.ReplaceExisting);

                return new OperationResult(true, "", false);
            }
            catch (OperationCanceledException) {
                await TryDeleteTempZip(zipFile);
                return new OperationResult(false, "Backup cancelled", false);
            }
            catch (Exception e) {
                await TryDeleteTempZip(zipFile);
                string error = $"Archive failed - {e.Message}";
                logger.LogError(error);
                return new OperationResult(false, error, true);
            }
        }

        private async Task TryDeleteTempZip(StorageFile? tempZipFile) {
            if (tempZipFile == null) return;

            try {
                await tempZipFile.DeleteAsync();
            }
            catch (Exception e){
                logger.LogWarning($"Failed to delete temp zip: '{e.Message}'");
                // Best effort cleanup; leaving the temp file behind is not data loss.
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
                string normalizedDestinationRoot = Path.GetFullPath(destinationFolder);
                if (!normalizedDestinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                    normalizedDestinationRoot += Path.DirectorySeparatorChar;
                }

                using (Stream zipStream = await zipResult.File.OpenStreamForReadAsync()) {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, false, Encoding.UTF8)) {
                        foreach(ZipArchiveEntry entry in archive.Entries) {
                            cancellationToken.ThrowIfCancellationRequested();

                            string destinationPath = Path.GetFullPath(Path.Combine(destinationFolder, entry.FullName));
                            if (!destinationPath.StartsWith(normalizedDestinationRoot, StringComparison.OrdinalIgnoreCase)) {
                                return new OperationResult(false, $"Zip entry is outside of the destination folder: '{entry.FullName}'", true);
                            }

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
                return new OperationResult(false, "Extract cancelled", false);
            }
            catch (Exception e) {
                string error = $"Unzip failed: {e.Message}";
                logger.LogError(error);
                return new OperationResult(false, error, true);
            }
        }
    }
}
