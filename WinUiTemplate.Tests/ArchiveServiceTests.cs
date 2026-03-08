using FluentAssertions;
using Moq;
using System.IO.Compression;
using Windows.Storage;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Tests
{
    public class ArchiveServiceTests
    {
        // Services & Stores
        private readonly Mock<IFileUtils> mockFileUtils;
        private readonly Mock<ILoggerService> mockLogger;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly ArchiveService archiveService;

        // Constructors

        public ArchiveServiceTests() {
            mockFileUtils = new Mock<IFileUtils>();
            mockLogger = new Mock<ILoggerService>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(mockFileUtils.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(mockLogger.Object);

            archiveService = new ArchiveService(mockServiceProvider.Object);
        }

        // Helper Classes

        /// <summary>
        /// Manages temporary test resources with automatic cleanup
        /// </summary>
        private class TempTestResources : IAsyncDisposable
        {
            public StorageFolder? SourceFolder { get; set; }
            public StorageFolder? ParentFolder { get; set; }
            public StorageFolder? DestinationFolder { get; set; }
            public StorageFile? ZipFile { get; set; }
            public StorageFile? TextFile { get; set; }

            public async ValueTask DisposeAsync() {
                if (ZipFile != null) await TestUtils.CleanupTempZipFile(ZipFile);
                if (SourceFolder != null) await TestUtils.CleanupTempFolder(SourceFolder);
                if (ParentFolder != null) await TestUtils.CleanupTempFolder(ParentFolder);
                if (DestinationFolder != null) await TestUtils.CleanupTempFolder(DestinationFolder);
            }
        }

        // Helper Methods - Resource Creation

        private async Task<TempTestResources> CreateZipTestResourcesAsync(bool createTextFile = false) {
            TempTestResources resources = new TempTestResources
            {
                SourceFolder = await TestUtils.GetTempFolder(),
                ParentFolder = await TestUtils.GetTempFolder()
            };

            resources.ZipFile = await TestUtils.GetTempZipFile(resources.ParentFolder);

            if (createTextFile) {
                resources.TextFile = await StorageFile.GetFileFromPathAsync(
                    Path.Combine(resources.SourceFolder.Path, "subfolder", "test.txt")
                );
            }

            return resources;
        }

        private async Task<TempTestResources> CreateExtractTestResourcesAsync() {
            TempTestResources resources = new TempTestResources
            {
                ParentFolder = await TestUtils.GetTempFolder(),
                DestinationFolder = await TestUtils.GetTempFolder()
            };

            resources.ZipFile = await TestUtils.GetTempZipFile(resources.ParentFolder);

            return resources;
        }

        // Helper Methods - Mock Setup

        private void SetupSuccessfulFileAccess(string filePath, StorageFile file) {
            mockFileUtils
                .Setup(x => x.TryGetFileAsync(filePath))
                .ReturnsAsync(new FileResult(true, null, file));
        }

        private void SetupFailedFileAccess(string filePath) {
            mockFileUtils
                .Setup(x => x.TryGetFileAsync(filePath))
                .ReturnsAsync(new FileResult(false, null, null));
        }

        private void SetupSuccessfulFolderAccess(string folderPath, StorageFolder folder) {
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(folderPath))
                .ReturnsAsync(new FolderResult(true, null, folder));
        }

        private void SetupFailedFolderAccess(string folderPath) {
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(folderPath))
                .ReturnsAsync(new FolderResult(false, null, null));
        }

        private void SetupSuccessfulGetAllFiles(string folderPath, List<StorageFile> files) {
            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(folderPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilesResult(true, null, files));
        }

        private void SetupFailedGetAllFiles(string folderPath) {
            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(folderPath, default))
                .ReturnsAsync(new FilesResult(false, null, null));
        }

        private void SetupGetRelativePath(string basePath, string filePath, string relativePath) {
            mockFileUtils
                .Setup(x => x.GetRelativePath(basePath, filePath))
                .Returns(relativePath);
        }

        private void SetupSubfolderCreation(string destinationPath) {
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(
                    It.Is<string>(path => path != destinationPath && path.Contains(destinationPath))
                ))
                .Returns(async (string path) => {
                    Directory.CreateDirectory(path);
                    StorageFolder? folder = await StorageFolder.GetFolderFromPathAsync(path);
                    return new FolderResult(true, null, folder);
                });
        }

        // Helper Methods - Common Setup Scenarios

        private void SetupSuccessfulZipScenario(TempTestResources resources) {
            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupSuccessfulFolderAccess(resources.SourceFolder!.Path, resources.SourceFolder);
            SetupSuccessfulFolderAccess(resources.ParentFolder!.Path, resources.ParentFolder);
            
            if (resources.TextFile != null) {
                SetupSuccessfulGetAllFiles(
                    resources.SourceFolder.Path, 
                    new List<StorageFile> { resources.TextFile }
                );
                SetupGetRelativePath(
                    resources.SourceFolder.Path, 
                    resources.TextFile.Path, 
                    "test.txt"
                );
            }
        }

        private void SetupSuccessfulExtractScenario(TempTestResources resources) {
            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupSuccessfulFolderAccess(resources.DestinationFolder!.Path, resources.DestinationFolder);
            SetupSubfolderCreation(resources.DestinationFolder.Path);
        }

        // Tests

        #region ZipFolderAsync Tests

        [Fact]
        public async Task ZipFolderAsync_SuccessfulZipCreation_ReturnsSuccess() {
            await using TempTestResources resources = await CreateZipTestResourcesAsync(createTextFile: true);

            SetupSuccessfulZipScenario(resources);

            OperationResult result = await archiveService.ZipFolderAsync(
                resources.SourceFolder!.Path, 
                resources.ZipFile!.Path
            );

            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task ZipFolderAsync_SourceFolderDoesNotExist_ReturnsFailure() {
            string sourceFolder = @"C:\NonExistent";
            string zipPath = @"C:\output.zip";

            SetupFailedFileAccess(zipPath);
            SetupFailedFolderAccess(sourceFolder);

            OperationResult result = await archiveService.ZipFolderAsync(sourceFolder, zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to access sourceFolder");
        }

        [Fact]
        public async Task ZipFolderAsync_ParentDirectoryCreationFails_ReturnsFailure() {
            string zipPath = @"C:\Output\output.zip";
            string sourceFolder = @"C:\Source";
            await using TempTestResources resources = new TempTestResources
            {
                SourceFolder = await TestUtils.GetTempFolder()
            };

            SetupFailedFileAccess(zipPath);
            SetupSuccessfulFolderAccess(sourceFolder, resources.SourceFolder);
            SetupFailedFolderAccess(@"C:\Output");

            OperationResult result = await archiveService.ZipFolderAsync(sourceFolder, zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create zip parent directory");
        }

        [Fact]
        public async Task ZipFolderAsync_ZipFileCreationFails_ReturnsFailure() {
            await using TempTestResources resources = new TempTestResources
            {
                SourceFolder = await TestUtils.GetTempFolder()
            };
            string zipFile = @"";

            SetupFailedFileAccess(zipFile);
            SetupSuccessfulFolderAccess(resources.SourceFolder.Path, resources.SourceFolder);

            OperationResult result = await archiveService.ZipFolderAsync(resources.SourceFolder.Path, zipFile);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create zip file");
        }

        [Fact]
        public async Task ZipFolderAsync_GetFilesInSourceFolderFails_ReturnsFailure() {
            StorageFolder tempSourceFolder = await TestUtils.GetTempFolder();
            StorageFolder tempParentFolder = await TestUtils.GetTempFolder();
            string zipPath = Path.Combine(tempParentFolder.Path, "output.zip");

            SetupFailedFileAccess(zipPath);
            SetupSuccessfulFolderAccess(tempSourceFolder.Path, tempSourceFolder);
            SetupSuccessfulFolderAccess(tempParentFolder.Path, tempParentFolder);
            SetupFailedGetAllFiles(tempSourceFolder.Path);

            try {
                OperationResult result = await archiveService.ZipFolderAsync(
                    tempSourceFolder.Path, 
                    zipPath
                );

                result.Success.Should().BeFalse();
                result.ErrorMessage.Should().Be("Failed to get files in source folder");
            }
            finally {
                await TestUtils.CleanupTempFolder(tempSourceFolder);
                await TestUtils.CleanupTempFolder(tempParentFolder);
            }
        }

        [Fact]
        public async Task ZipFolderAsync_CancellationRequested_ReturnsCancelledResult() {
            StorageFolder tempSourceFolder = await TestUtils.GetTempFolder();
            StorageFolder tempParentFolder = await TestUtils.GetTempFolder();
            string zipPath = Path.Combine(tempParentFolder.Path, "output.zip");
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            SetupFailedFileAccess(zipPath);
            SetupSuccessfulFolderAccess(tempSourceFolder.Path, tempSourceFolder);
            SetupSuccessfulFolderAccess(tempParentFolder.Path, tempParentFolder);

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(tempSourceFolder.Path, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            try {
                OperationResult result = await archiveService.ZipFolderAsync(
                    tempSourceFolder.Path, 
                    zipPath, 
                    tokenSource.Token
                );

                result.Success.Should().BeFalse();
                result.ErrorMessage.Should().Be("Backup cancelled");
            }
            finally {
                await TestUtils.CleanupTempFolder(tempSourceFolder);
                await TestUtils.CleanupTempFolder(tempParentFolder);
            }
        }

        [Fact]
        public async Task ZipFolderAsync_DeletesExistingZipFile_WhenZipAlreadyExists() {
            await using TempTestResources resources = await CreateZipTestResourcesAsync();

            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupFailedFolderAccess(resources.SourceFolder!.Path);

            await archiveService.ZipFolderAsync(resources.SourceFolder.Path, resources.ZipFile.Path);

            File.Exists(resources.ZipFile.Path).Should().BeFalse();
        }

        [Fact]
        public async Task ZipFolderAsync_ExceptionThrown_LogsErrorAndReturnsFailure() {
            await using TempTestResources resources = await CreateZipTestResourcesAsync();

            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupSuccessfulFolderAccess(resources.SourceFolder!.Path, resources.SourceFolder);
            SetupSuccessfulFolderAccess(resources.ParentFolder!.Path, resources.ParentFolder);

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(resources.SourceFolder.Path, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Disc full"));

            OperationResult result = await archiveService.ZipFolderAsync(
                resources.SourceFolder.Path, 
                resources.ZipFile.Path
            );

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Archive failed");
            result.ErrorMessage.Should().Contain("Disc full");
            mockLogger.Verify(x => x.LogError(It.IsAny<string>(), null, true), Times.Once);
        }

        #endregion

        #region ExtractZip Tests

        [Fact]
        public async Task ExtractZip_SuccessfulZipExtraction_ReturnsSuccess() {
            await using TempTestResources resources = await CreateExtractTestResourcesAsync();

            SetupSuccessfulExtractScenario(resources);

            OperationResult result = await archiveService.ExtractZip(
                resources.ZipFile!.Path, 
                resources.DestinationFolder!.Path
            );

            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeEmpty();

            // Verify extracted files
            string dest = resources.DestinationFolder.Path;
            string subfolder = Path.Combine(dest, "subfolder");
            string testFile = Path.Combine(subfolder, "test.txt");

            Directory.Exists(dest).Should().BeTrue();
            Directory.Exists(subfolder).Should().BeTrue();
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public async Task ExtractZip_ZipFileDoesNotExist_ReturnsFailure() {
            string zipPath = @"C:\input.zip";
            string destination = @"C:\Extract";

            SetupFailedFileAccess(zipPath);

            OperationResult result = await archiveService.ExtractZip(zipPath, destination);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to access zip file");
        }

        [Fact]
        public async Task ExtractZip_CouldNotCreateDestinationFolder_ReturnsFailure() {
            await using TempTestResources resources = await CreateExtractTestResourcesAsync();
            string destination = @"C:\Extract";

            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupFailedFolderAccess(destination);

            OperationResult result = await archiveService.ExtractZip(resources.ZipFile.Path, destination);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create or get destination folder");
        }

        [Fact]
        public async Task ExtractZip_CancellationRequested_ReturnsCancelledResult() {
            await using TempTestResources resources = await CreateExtractTestResourcesAsync();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupSuccessfulFolderAccess(resources.DestinationFolder!.Path, resources.DestinationFolder);

            OperationResult result = await archiveService.ExtractZip(
                resources.ZipFile.Path, 
                resources.DestinationFolder.Path, 
                tokenSource.Token
            );

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Extract cancelled");
        }

        [Fact]
        public async Task ExtractZip_ExceptionThrown_LogsErrorAndReturnsFailure() {
            await using TempTestResources resources = await CreateExtractTestResourcesAsync();

            SetupSuccessfulFileAccess(resources.ZipFile!.Path, resources.ZipFile);
            SetupSuccessfulFolderAccess(resources.DestinationFolder!.Path, resources.DestinationFolder);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(
                    It.Is<string>(path => 
                        path.Contains(resources.DestinationFolder.Path) && 
                        path != resources.DestinationFolder.Path
                    )
                ))
                .ThrowsAsync(new IOException("Disc full"));

            OperationResult result = await archiveService.ExtractZip(
                resources.ZipFile.Path, 
                resources.DestinationFolder.Path
            );

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Unzip failed");
            result.ErrorMessage.Should().Contain("Disc full");
            mockLogger.Verify(x => x.LogError(It.IsAny<string>(), null, true), Times.Once);
        }

        #endregion
    }
}
