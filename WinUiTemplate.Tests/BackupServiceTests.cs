using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Tests
{
    public class BackupServiceTests
    {
        // Services & Stores
        private readonly Mock<IProgramData> mockProgramData;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IUserSettings> mockUserSettings;
        private readonly Mock<IFileUtils> mockFileUtils;
        private readonly Mock<IDialogService> mockDialogService;
        private readonly Mock<INotificationService> mockNotificationService;
        private readonly Mock<ILoggerService> mockLoggerService;
        private readonly Mock<IArchiveService> mockArchiveService;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly Mock<IEncryptionService> mockEncryptionService;
        private readonly BackupService backupService;

        // Constructors

        public BackupServiceTests() {
            mockProgramData = new Mock<IProgramData>();
            mockFilePaths = new Mock<IFilePaths>();
            mockUserSettings = new Mock<IUserSettings>();
            mockFileUtils = new Mock<IFileUtils>();
            mockDialogService = new Mock<IDialogService>();
            mockNotificationService = new Mock<INotificationService>();
            mockLoggerService = new Mock<ILoggerService>();
            mockArchiveService = new Mock<IArchiveService>();
            mockEncryptionService = new Mock<IEncryptionService>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockProgramData.Setup(x => x.FilePaths).Returns(mockFilePaths.Object);

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IProgramData)))
                .Returns(mockProgramData.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(mockFileUtils.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IDialogService)))
                .Returns(mockDialogService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(INotificationService)))
                .Returns(mockNotificationService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(mockLoggerService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IArchiveService)))
                .Returns(mockArchiveService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IEncryptionService)))
                .Returns(mockEncryptionService.Object);

            backupService = new BackupService(mockServiceProvider.Object);
        }

        // Helper Classes

        /// <summary>
        /// Manages temporary test resources with automatic cleanup
        /// </summary>
        private class TempTestResources : IAsyncDisposable
        {
            public StorageFolder? BackupsFolder { get; set; }
            public StorageFolder? DataFolder { get; set; }
            public StorageFolder? RootFolder { get; set; }
            public StorageFolder? TempFolder { get; set; }

            public async ValueTask DisposeAsync() {
                if (BackupsFolder != null) await TestUtils.CleanupTempFolder(BackupsFolder);
                if (DataFolder != null) await TestUtils.CleanupTempFolder(DataFolder);
                if (RootFolder != null) await TestUtils.CleanupTempFolder(RootFolder);
                if (TempFolder != null) await TestUtils.CleanupTempFolder(TempFolder);
            }
        }

        // Tests

        #region CreateBackupAsync Tests

        [Fact]
        public async Task CreateBackupAsync_CreatesZipAndMetadata() {
            await using var resources = new TempTestResources
            {
                BackupsFolder = await TestUtils.GetTempFolder(),
                DataFolder = await TestUtils.GetTempFolder()
            };
            string metadataPath = Path.Combine(resources.DataFolder.Path, "metadata.json");

            mockFilePaths.SetupGet(x => x.RootFolder).Returns(resources.DataFolder.Path);
            mockFilePaths.SetupGet(x => x.TempMetadataFile).Returns(metadataPath);

            mockProgramData.SetupGet(x => x.EnableBackups).Returns(true);
            mockProgramData.Setup(x => x.ProgramVersion).Returns(new Version("3.2.1"));

            mockUserSettings.SetupGet(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.SetupGet(x => x.BackupsFolder).Returns(resources.BackupsFolder.Path);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(It.IsAny<string>()))
                .Returns(async (string path) => {
                    Directory.CreateDirectory(path);
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                    return new FolderResult(true, "", folder);
                });

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(resources.DataFolder.Path, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilesResult(true, "", new List<StorageFile>() {
                    await StorageFile.GetFileFromPathAsync(Path.Combine(resources.DataFolder.Path, "subfolder", "test.txt"))
                }));

            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((string path, string content, bool? encrypt) => {
                    try {
                        string? directory = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(directory)) {
                            Directory.CreateDirectory(directory);
                        }
                        File.WriteAllText(path, content);
                        return new FileWriteResult(true, "");
                    }
                    catch (Exception ex) {
                        return new FileWriteResult(false, ex.Message);
                    }
                });

            mockArchiveService
                .Setup(x => x.ZipFolderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string sourceFolder, string destinationZip, CancellationToken token) => {
                    try {
                        string? directory = Path.GetDirectoryName(destinationZip);
                        if (!string.IsNullOrEmpty(directory)) {
                            Directory.CreateDirectory(directory);
                        }
                        if (Directory.Exists(sourceFolder)) {
                            System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder, destinationZip);
                        }
                        return new OperationResult(true, "", false);
                    }
                    catch (OperationCanceledException) {
                        return new OperationResult(false, "Cancelled", false);
                    }
                    catch (Exception ex) {
                        return new OperationResult(false, ex.Message, false);
                    }
                });

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeTrue();
            string[] zipFiles = Directory.GetFiles(resources.BackupsFolder.Path, "*.zip");
            zipFiles.Count().Should().Be(1);

            string jsonContent = await File.ReadAllTextAsync(metadataPath);
            jsonContent.Should().NotBeNullOrEmpty();

            BackupInfo? metadata = JsonConvert.DeserializeObject<BackupInfo>(jsonContent);
            metadata.Should().NotBeNull();
            metadata.CreatedWith.Should().Be(new Version("3.2.1"));
            metadata.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateBackupAsync_RespectsBackupsDisabledViaProgramData() {
            mockProgramData.Setup(x => x.EnableBackups).Returns(false);

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backups are disabled");
        }

        [Fact]
        public async Task CreateBackupAsync_RespectsBackupsDisabledViaUserSettings() {
            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(false);

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backups are disabled");
        }

        [Fact]
        public async Task CreateBackupAsync_ChecksBackupsFolderConfigured() {
            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder ).Returns("");

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backups folder not configured");
        }

        [Fact]
        public async Task CreateBackupAsync_ValidateBackupsFolder_Fail() {
            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("asdasdasd");

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("asdasdasd"))
                .ReturnsAsync(new FolderResult(false, "Invalid path", null));

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to get or create backups folder");
        }

        [Fact]
        public async Task CreateBackupAsync_ValidateBackupsFolder_Pass() {
            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns(resources.TempFolder.Path);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(resources.TempFolder.Path))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            OperationResult result = await backupService.CreateBackupAsync();

            result.ErrorMessage.Should().NotBe("Backups folder is not configured");
            result.ErrorMessage.Should().NotContain("Failed to get or create backups folder");
        }

        [Fact]
        public async Task CreateBackupAsync_WritesMetadataFile_AbortOnFail() {
            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns(resources.TempFolder.Path);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(resources.TempFolder.Path))
                .ReturnsAsync(new FolderResult(true, null, resources.TempFolder));

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilesResult(false, null, null));

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create backup metadata file");
        }

        [Fact]
        public async Task CreateBackupAsync_CancellationCheckAtStart() {
            StorageFolder temp = await TestUtils.GetTempFolder();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            OperationResult result = await backupService.CreateBackupAsync(tokenSource.Token);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backup cancelled");
        }

        [Fact]
        public async Task CreateBackupAsync_CancellationDuringFileOperations() {
            StorageFolder temp = await TestUtils.GetTempFolder();
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns(temp.Path);

            mockFilePaths.Setup(x => x.RootFolder).Returns(temp.Path);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(temp.Path))
                .ThrowsAsync(new OperationCanceledException());

            OperationResult result = await backupService.CreateBackupAsync(tokenSource.Token);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backup cancelled");
        }

        [Fact]
        public async Task CreateBackupAsync_FailsOnException() {
            StorageFolder temp = await TestUtils.GetTempFolder();

            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns(temp.Path);

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(temp.Path))
                .ReturnsAsync(new FolderResult(true, null, temp));

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(It.IsAny<string>()))
                .ThrowsAsync(new IOException("Disc full"));

            OperationResult result = await backupService.CreateBackupAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Backup creation failed");
        }

        #endregion

        #region RestoreBackupAsync Tests

        [Fact]
        public async Task RestoreBackupAsync_UserCancelsInitialConfirmation() {
            mockDialogService
                .Setup(x => x.Confirm("Restore Backup?", It.IsAny<string>()))
                .ReturnsAsync(false);

            OperationResult result = await backupService.RestoreBackupAsync("C:\\test\\backup.zip");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User cancelled");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenCreateBackupBeforeRestoreFails() {
            mockDialogService
                .Setup(x => x.Confirm("Restore Backup?", It.IsAny<string>()))
                .ReturnsAsync(true);

            mockProgramData.Setup(x => x.EnableBackups).Returns(false);

            OperationResult result = await backupService.RestoreBackupAsync("C:\\test\\backup.zip");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create backup");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsOnInvalidZipPath() {
            mockDialogService
                .Setup(x => x.Confirm("Restore Backup?", It.IsAny<string>()))
                .ReturnsAsync(true);

            await SetupSuccessfulCreateBackupAsync();

            OperationResult resultEmpty = await backupService.RestoreBackupAsync("");
            resultEmpty.Success.Should().BeFalse();
            resultEmpty.ErrorMessage.Should().Contain("Invalid backup path");

            OperationResult resultWhitespace = await backupService.RestoreBackupAsync("   ");
            resultWhitespace.Success.Should().BeFalse();
            resultWhitespace.ErrorMessage.Should().Contain("Invalid backup path");

            OperationResult resultNull = await backupService.RestoreBackupAsync(null);
            resultNull.Success.Should().BeFalse();
            resultNull.ErrorMessage.Should().Contain("Invalid backup path");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenBackupFileNotAccessible() {
            mockDialogService
                .Setup(x => x.Confirm("Restore Backup?", It.IsAny<string>()))
                .ReturnsAsync(true);

            await SetupSuccessfulCreateBackupAsync();

            mockFileUtils
                .Setup(x => x.TryGetFileAsync("C:\\test\\backup.zip"))
                .ReturnsAsync(new FileResult(false, "File not found", null));

            OperationResult result = await backupService.RestoreBackupAsync("C:\\test\\backup.zip");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backup file not accessible");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenBackupMetadataCannotBeRead() {
            SetupRestoreConfirmation();
            await SetupSuccessfulCreateBackupAsync();

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithCustomContent(resources.TempFolder, "somefile.txt", "test content", "invalid.zip");
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to read backup metadata");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenBackupCreatedWithNewerVersion() {
            SetupRestoreConfirmation();
            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("2.0.0"), "newer_version.zip");
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Backup was created with a newer version");
        }

        [Fact]
        public async Task RestoreBackupAsync_UserCancelsOutdatedVersionWarning() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.Confirm("Use Outdated Backup?", It.IsAny<string>()))
                .ReturnsAsync(false);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("2.0.0"));

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "older_version.zip");
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User cancelled due to outdated data");
        }

        [Fact]
        public async Task RestoreBackupAsync_ProceedsWhenUserConfirmsOutdatedVersion() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.Confirm("Use Outdated Backup?", It.IsAny<string>()))
                .ReturnsAsync(true);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("2.0.0"), "Test App", "C:\\InvalidRootFolder", "C:\\InvalidRootFolder\\metadata.json");
            SetupRootFolderAccess("C:\\InvalidRootFolder", null, false, "Cannot access root folder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "older_version.zip");
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Root folder not accessible");
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenRootFolderNotAccessible() {
            SetupRestoreConfirmation();
            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"), "Test App", "C:\\InvalidRootFolder", "C:\\InvalidRootFolder\\metadata.json");
            SetupRootFolderAccess("C:\\InvalidRootFolder", null, false, "Cannot access root folder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Root folder not accessible");
        }

        [Fact]
        public async Task RestoreBackupAsync_PausesLogger() {
            SetupRestoreConfirmation();
            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", false, "Extraction failed");

            await backupService.RestoreBackupAsync(zipPath);

            mockLoggerService.Verify(x => x.Pause(), Times.Once);
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenExtractZipFails() {
            SetupRestoreConfirmation();
            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", false, "Corrupted archive");

            OperationResult result = await backupService.RestoreBackupAsync(zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Corrupted archive");
        }

        [Fact]
        public async Task RestoreBackupAsync_SucceedsWhenMetadataFileNotAccessible() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.ShowMessage(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);
            SetupFileAccess("C:\\RootFolder\\metadata.json", null, false, "File not found");

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", true);

            try {
                OperationResult result = await backupService.RestoreBackupAsync(zipPath);
                result.Success.Should().BeTrue();
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80040154)) {
                mockDialogService.Verify(x => x.ShowMessage(MessageType.Success, "Restore Sucessful", It.IsAny<string>()), Times.Once);
            }
        }

        [Fact]
        public async Task RestoreBackupAsync_SucceedsWhenMetadataFileDeletionFails() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.ShowMessage(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            StorageFile metadataFile = await resources.RootFolder.CreateFileAsync("metadata.json");

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);
            SetupFileAccess("C:\\RootFolder\\metadata.json", metadataFile);

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", true);

            try {
                OperationResult result = await backupService.RestoreBackupAsync(zipPath);
                result.Success.Should().BeTrue();
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80040154)) {
                mockDialogService.Verify(x => x.ShowMessage(MessageType.Success, "Restore Sucessful", It.IsAny<string>()), Times.Once);
            }
        }

        [Fact]
        public async Task RestoreBackupAsync_ShowsSuccessDialogAndRestartsApp() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.ShowMessage(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);
            SetupFileAccess("C:\\RootFolder\\metadata.json", null, false, "File not found");

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", true);

            try {
                await backupService.RestoreBackupAsync(zipPath);
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80040154)) {
            }

            mockDialogService.Verify(x => x.ShowMessage(MessageType.Success, "Restore Sucessful", "Test App will now restart."), Times.Once);
        }

        [Fact]
        public async Task RestoreBackupAsync_SuccessfulFullRestore() {
            SetupRestoreConfirmation();
            mockDialogService
                .Setup(x => x.ShowMessage(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await SetupSuccessfulCreateBackupAsync();
            SetupBasicRestoreConfiguration(new Version("1.0.0"));

            await using var resources = new TempTestResources
            {
                RootFolder = await TestUtils.GetTempFolder(),
                TempFolder = await TestUtils.GetTempFolder()
            };

            StorageFile metadataFile = await resources.RootFolder.CreateFileAsync("metadata.json");

            SetupRootFolderAccess("C:\\RootFolder", resources.RootFolder);
            SetupFileAccess("C:\\RootFolder\\metadata.json", metadataFile);

            string zipPath = Path.Combine(resources.TempFolder.Path, "backup.zip");

            using (var zipArchive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry("metadata.json");
                using (var writer = new StreamWriter(entry.Open())) {
                    BackupInfo backupInfo = new BackupInfo(zipPath, DateTime.Now, new Version("1.0.0"), 1024);
                    string json = JsonConvert.SerializeObject(backupInfo);
                    await writer.WriteAsync(json);
                }
                var dataEntry = zipArchive.CreateEntry("data.txt");
                using (var dataWriter = new StreamWriter(dataEntry.Open())) {
                    await dataWriter.WriteAsync("test data");
                }
            }

            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);
            SetupArchiveExtraction(zipPath, "C:\\RootFolder", true);

            try {
                OperationResult result = await backupService.RestoreBackupAsync(zipPath);
                result.Success.Should().BeTrue();
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80040154)) {
            }

            mockDialogService.Verify(x => x.Confirm("Restore Backup?", It.IsAny<string>()), Times.Once);
            mockLoggerService.Verify(x => x.Pause(), Times.Once);
            mockArchiveService.Verify(x => x.ExtractZip(zipPath, "C:\\RootFolder", It.IsAny<CancellationToken>()), Times.Once);
            mockDialogService.Verify(x => x.ShowMessage(MessageType.Success, "Restore Sucessful", "Test App will now restart."), Times.Once);
        }


        // Private Functions

        private async Task SetupSuccessfulCreateBackupAsync() {
            mockProgramData.Setup(x => x.EnableBackups).Returns(true);
            mockUserSettings.Setup(x => x.AutomaticBackups).Returns(true);
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\temp");
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(It.IsAny<string>()))
                .ReturnsAsync(new FolderResult(true, "", await TestUtils.GetTempFolder()));
            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilesResult(true, "", new List<StorageFile>()));
            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new FileWriteResult(true, ""));
            mockArchiveService
                .Setup(x => x.ZipFolderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OperationResult(true, "", false));
        }

        private async Task<string> CreateZipWithMetadata(StorageFolder folder, Version version, string zipFileName = "backup.zip", long size = 1024) {
            string zipPath = Path.Combine(folder.Path, zipFileName);

            using (var zipArchive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry("metadata.json");
                using (var writer = new StreamWriter(entry.Open())) {
                    BackupInfo backupInfo = new BackupInfo(zipPath, DateTime.Now, version, size);
                    string json = JsonConvert.SerializeObject(backupInfo);
                    await writer.WriteAsync(json);
                }
            }

            return zipPath;
        }

        private async Task<string> CreateZipWithCustomContent(StorageFolder folder, string fileName, string content, string zipFileName = "backup.zip") {
            string zipPath = Path.Combine(folder.Path, zipFileName);

            using (var zipArchive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry(fileName);
                using (var writer = new StreamWriter(entry.Open())) {
                    await writer.WriteAsync(content);
                }
            }

            return zipPath;
        }

        private void SetupRestoreConfirmation(bool confirm = true) {
            mockDialogService
                .Setup(x => x.Confirm("Restore Backup?", It.IsAny<string>()))
                .ReturnsAsync(confirm);
        }

        private void SetupDeleteConfirmation(bool confirm = true) {
            mockDialogService
                .Setup(x => x.Confirm("Delete Backup?", "This cannot be undone."))
                .ReturnsAsync(confirm);
        }

        private void SetupFileAccess(string filePath, StorageFile? file, bool success = true, string errorMessage = "") {
            mockFileUtils
                .Setup(x => x.TryGetFileAsync(filePath))
                .ReturnsAsync(new FileResult(success, errorMessage, file));
        }

        private void SetupRootFolderAccess(string rootFolderPath, StorageFolder? folder, bool success = true, string errorMessage = "") {
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(rootFolderPath))
                .ReturnsAsync(new FolderResult(success, errorMessage, folder));
        }

        private void SetupArchiveExtraction(string zipPath, string rootFolder, bool success, string errorMessage = "") {
            mockArchiveService
                .Setup(x => x.ExtractZip(zipPath, rootFolder, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OperationResult(success, errorMessage, false));
        }

        private void SetupBasicRestoreConfiguration(Version programVersion, string programName = "Test App", string rootFolder = "C:\\RootFolder", string tempMetadataPath = "C:\\RootFolder\\metadata.json") {
            mockProgramData.Setup(x => x.ProgramVersion).Returns(programVersion);
            mockProgramData.Setup(x => x.ProgramName).Returns(programName);
            mockFilePaths.Setup(x => x.RootFolder).Returns(rootFolder);
            mockFilePaths.Setup(x => x.TempMetadataFile).Returns(tempMetadataPath);
        }

        #endregion

        #region DeleteBackupAsync Tests

        [Fact]
        public async Task DeleteBackupAsync_FailsOnInvalidZipPath() {
            OperationResult resultEmpty = await backupService.DeleteBackupAsync("");
            resultEmpty.Success.Should().BeFalse();
            resultEmpty.ErrorMessage.Should().Be("Invalid backup path");

            OperationResult resultWhitespace = await backupService.DeleteBackupAsync("   ");
            resultWhitespace.Success.Should().BeFalse();
            resultWhitespace.ErrorMessage.Should().Be("Invalid backup path");

            OperationResult resultNull = await backupService.DeleteBackupAsync(null);
            resultNull.Success.Should().BeFalse();
            resultNull.ErrorMessage.Should().Be("Invalid backup path");
        }

        [Fact]
        public async Task DeleteBackupAsync_FailsWhenUserCancelsConfirmation() {
            SetupDeleteConfirmation(false);

            OperationResult result = await backupService.DeleteBackupAsync("C:\\test\\backup.zip");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User cancelled");
        }

        [Fact]
        public async Task DeleteBackupAsync_FailsWhenZipFileNotAccessible() {
            SetupDeleteConfirmation();
            SetupFileAccess("C:\\test\\backup.zip", null, false, "File not found");

            OperationResult result = await backupService.DeleteBackupAsync("C:\\test\\backup.zip");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Zip file not accessable");
        }

        [Fact]
        public async Task DeleteBackupAsync_SuccessfullyDeletesBackup() {
            SetupDeleteConfirmation();

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));
            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);

            SetupFileAccess(zipPath, zipFile);

            bool eventInvoked = false;
            backupService.BackupDeleted += () => eventInvoked = true;

            OperationResult result = await backupService.DeleteBackupAsync(zipPath);

            result.Success.Should().BeTrue();
            File.Exists(zipPath).Should().BeFalse();
            eventInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteBackupAsync_FailsOnException() {
            SetupDeleteConfirmation();

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            string zipPath = await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"));

            FileStream? lockStream = null;
            try {
                lockStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None);

                StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);
                SetupFileAccess(zipPath, zipFile);

                OperationResult result = await backupService.DeleteBackupAsync(zipPath);

                result.Success.Should().BeFalse();
                result.ErrorMessage.Should().NotBeNullOrEmpty();
                mockLoggerService.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Backup deletion failed"))), Times.Once);
                mockNotificationService.Verify(x => x.Notify(InfoBarSeverity.Warning, "Delete Failed", It.IsAny<string>()), Times.Once);
            }
            finally {
                lockStream?.Dispose();
            }
        }

        #endregion

        #region GetBackupAsync Tests

        #endregion

        #region GetBackupsAsync Tests

        [Fact]
        public async Task GetBackupsAsync_ReturnsEmptyWhenBackupsFolderNotConfigured() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("");

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBackupsAsync_ReturnsEmptyWhenValidateBackupFolderFails() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\InvalidPath");
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\InvalidPath"))
                .ReturnsAsync(new FolderResult(false, "Access denied", null));

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBackupsAsync_ReturnsEmptyWhenBackupFolderIsNull() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");
            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(false, "Failed", null));

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBackupsAsync_FiltersNonZipFiles() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "backup1.zip");
            await resources.TempFolder.CreateFileAsync("readme.txt");
            await resources.TempFolder.CreateFileAsync("backup.bak");

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Count.Should().Be(1);
            backups[0].Path.Should().EndWith("backup1.zip");
        }

        [Fact]
        public async Task GetBackupsAsync_SkipsBackupsWithInvalidMetadata() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "valid.zip");
            await CreateZipWithCustomContent(resources.TempFolder, "data.txt", "no metadata", "invalid.zip");

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Count.Should().Be(1);
            backups[0].Path.Should().EndWith("valid.zip");
        }

        [Fact]
        public async Task GetBackupsAsync_HandlesExceptionsDuringMetadataRead() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "backup1.zip");
            await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "backup2.zip");

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetBackupsAsync_ReturnsMultipleValidBackups() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), "backup1.zip", 1024);
            await CreateZipWithMetadata(resources.TempFolder, new Version("1.5.0"), "backup2.zip", 2048);
            await CreateZipWithMetadata(resources.TempFolder, new Version("2.0.0"), "backup3.zip", 4096);

            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync();

            backups.Count.Should().Be(3);
            backups.Should().Contain(b => b.Path.EndsWith("backup1.zip") && b.CreatedWith == new Version("1.0.0"));
            backups.Should().Contain(b => b.Path.EndsWith("backup2.zip") && b.CreatedWith == new Version("1.5.0"));
            backups.Should().Contain(b => b.Path.EndsWith("backup3.zip") && b.CreatedWith == new Version("2.0.0"));
        }

        [Fact]
        public async Task GetBackupsAsync_RespectsCancellation() {
            mockUserSettings.Setup(x => x.BackupsFolder).Returns("C:\\BackupsFolder");

            await using var resources = new TempTestResources
            {
                TempFolder = await TestUtils.GetTempFolder()
            };

            mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync("C:\\BackupsFolder"))
                .ReturnsAsync(new FolderResult(true, "", resources.TempFolder));

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            for (int i = 0; i < 10; i++) {
                await CreateZipWithMetadata(resources.TempFolder, new Version("1.0.0"), $"backup{i}.zip");
            }

            tokenSource.Cancel();

            Func<Task> act = async () => await backupService.GetBackupsAsync(tokenSource.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

    }
}
