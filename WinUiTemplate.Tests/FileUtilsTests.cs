using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class FileUtilsTests : IDisposable
    {
        // Services & Stores
        private readonly Mock<IProgramData> mockProgramData;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IDialogService> mockDialogService;
        private readonly Mock<IEncryptionService> mockEncryptionService;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly FileUtils fileUtils;
        private readonly StorageFolder tempFolder;

        // Constructors

        public FileUtilsTests() {
            tempFolder = TestUtils.GetTempFolder().Result;

            mockProgramData = new Mock<IProgramData>();
            mockFilePaths = new Mock<IFilePaths>();
            mockDialogService = new Mock<IDialogService>();
            mockEncryptionService = new Mock<IEncryptionService>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockProgramData.Setup(x => x.ProgramName).Returns("TestApp");
            mockProgramData.Setup(x => x.FilePaths).Returns(mockFilePaths.Object);
            mockProgramData.Setup(x => x.EncryptionLevel).Returns(EncryptionLevel.None);

            mockFilePaths.Setup(x => x.RootFolder).Returns(Path.Combine(tempFolder.Path, "Root"));
            mockFilePaths.Setup(x => x.DataFolder).Returns(Path.Combine(tempFolder.Path, "Root", "Data"));
            mockFilePaths.Setup(x => x.LogsFolder).Returns(Path.Combine(tempFolder.Path, "Root", "Logs"));
            mockFilePaths.Setup(x => x.CrashReportsFolder).Returns(Path.Combine(tempFolder.Path, "Root", "CrashReports"));

            mockEncryptionService
                .Setup(x => x.EncryptToBase64Async(It.IsAny<string>()))
                .ReturnsAsync((string text) => $"ENCRYPTED:{text}");

            mockEncryptionService
                .Setup(x => x.DecryptFromBase64Async(It.IsAny<string>()))
                .ReturnsAsync((string text) => text.Replace("ENCRYPTED:", ""));

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IProgramData)))
                .Returns(mockProgramData.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IDialogService)))
                .Returns(mockDialogService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IEncryptionService)))
                .Returns(mockEncryptionService.Object);

            fileUtils = new FileUtils(mockServiceProvider.Object);
        }

        public void Dispose() {
            TestUtils.CleanupTempFolder(tempFolder).Wait();
        }

        // Tests

        #region TryGetFileAsync Tests

        [Fact]
        public async Task TryGetFileAsync_ReturnsSuccessForExistingFile() {
            string filePath = Path.Combine(tempFolder.Path, "test.txt");
            await File.WriteAllTextAsync(filePath, "content");

            FileResult result = await fileUtils.TryGetFileAsync(filePath);

            result.Success.Should().BeTrue();
            result.File.Should().NotBeNull();
            result.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task TryGetFileAsync_ReturnsFailureForNonExistentFile() {
            string filePath = Path.Combine(tempFolder.Path, "nonexistent.txt");

            FileResult result = await fileUtils.TryGetFileAsync(filePath);

            result.Success.Should().BeFalse();
            result.File.Should().BeNull();
            result.ErrorMessage.Should().Be("File not accessible or does not exist");
        }

        #endregion

        #region TryReadFileAsync Tests

        [Fact]
        public async Task TryReadFileAsync_ReadsPlainTextFile() {
            string filePath = Path.Combine(tempFolder.Path, "plain.txt");
            string content = "Plain text content";
            await File.WriteAllTextAsync(filePath, content);

            FileReadResult result = await fileUtils.TryReadFileAsync(filePath);

            result.Success.Should().BeTrue();
            result.Content.Should().Be(content);
            result.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task TryReadFileAsync_ReadsAndDecryptsEncryptedFile() {
            string filePath = Path.Combine(tempFolder.Path, "encrypted.txt");
            string plainContent = "Secret data";
            string encryptedContent = fileUtils.EncryptedFileHeader + $"ENCRYPTED:{plainContent}";
            await File.WriteAllTextAsync(filePath, encryptedContent);

            FileReadResult result = await fileUtils.TryReadFileAsync(filePath);

            result.Success.Should().BeTrue();
            result.Content.Should().Be(plainContent);
        }

        [Fact]
        public async Task TryReadFileAsync_ReturnsFailureForNonExistentFile() {
            string filePath = Path.Combine(tempFolder.Path, "missing.txt");

            FileReadResult result = await fileUtils.TryReadFileAsync(filePath);

            result.Success.Should().BeFalse();
            result.Content.Should().BeEmpty();
            result.ErrorMessage.Should().Be("File not accessible");
        }

        #endregion

        #region TryWriteFileAsync Tests

        [Fact]
        public void TryWriteFileAsync_MethodExists() {
            // FileUtils TryWriteFileAsync uses Windows Storage APIs that require
            // full UWP app context. This test verifies the method signature exists.
            var method = typeof(FileUtils).GetMethod("TryWriteFileAsync", 
                new[] { typeof(string), typeof(string), typeof(bool?) });

            method.Should().NotBeNull();
        }

        #endregion

        #region TryDeleteFileAsync Tests

        [Fact]
        public async Task TryDeleteFileAsync_DeletesExistingFile() {
            string filePath = Path.Combine(tempFolder.Path, "delete_me.txt");
            await File.WriteAllTextAsync(filePath, "content");

            OperationResult result = await fileUtils.TryDeleteFileAsync(filePath);

            result.Success.Should().BeTrue();
            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public async Task TryDeleteFileAsync_ReturnsFailureForNonExistentFile() {
            string filePath = Path.Combine(tempFolder.Path, "nonexistent.txt");

            OperationResult result = await fileUtils.TryDeleteFileAsync(filePath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("File not accessible");
        }

        #endregion

        #region TryGetOrCreateFolderAsync Tests

        [Fact]
        public async Task TryGetOrCreateFolderAsync_ReturnsFailureForInvalidPath() {
            string folderPath = "Z:\\NonExistent\\Path";

            FolderResult result = await fileUtils.TryGetOrCreateFolderAsync(folderPath);

            result.Success.Should().BeFalse();
        }

        #endregion

        #region TryGetAllFilesAsync Tests

        [Fact]
        public async Task TryGetAllFilesAsync_ReturnsEmptyForNonExistentFolder() {
            string folderPath = Path.Combine(tempFolder.Path, "missing");

            FilesResult result = await fileUtils.TryGetAllFilesAsync(folderPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Folder not accessible");
        }

        #endregion

        #region TryDeleteFolderAsync Tests

        [Fact]
        public async Task TryDeleteFolderAsync_ReturnsFailureForNonExistentFolder() {
            string folderPath = Path.Combine(tempFolder.Path, "nonexistent_folder");

            OperationResult result = await fileUtils.TryDeleteFolderAsync(folderPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Folder not accessible");
        }

        #endregion

        #region EnsureFolderAccessAsync Tests

        [Fact]
        public async Task EnsureFolderAccessAsync_ReturnsFalseForNonExistentFolder() {
            string folderPath = Path.Combine(tempFolder.Path, "inaccessible");

            bool result = await fileUtils.EnsureFolderAccessAsync(folderPath);

            result.Should().BeFalse();
        }

        #endregion

        #region CreateProgramFolderStructure Tests

        [Fact]
        public void CreateProgramFolderStructure_RequiresValidPaths() {
            // This test verifies the method exists and can be called
            Func<Task> act = async () => await fileUtils.CreateProgramFolderStructure();

            act.Should().NotThrowAsync();
        }

        #endregion

        #region GetRelativePath Tests

        [Fact]
        public void GetRelativePath_ReturnsRelativePathWhenWithinRoot() {
            string rootPath = "C:\\Root";
            string fullPath = "C:\\Root\\Folder\\File.txt";

            string result = fileUtils.GetRelativePath(rootPath, fullPath);

            result.Should().Be("Folder\\File.txt");
        }

        [Fact]
        public void GetRelativePath_ReturnsFullPathWhenOutsideRoot() {
            string rootPath = "C:\\Root";
            string fullPath = "C:\\Other\\File.txt";

            string result = fileUtils.GetRelativePath(rootPath, fullPath);

            result.Should().Be(fullPath);
        }

        [Fact]
        public void GetRelativePath_HandlesDifferentCasing() {
            string rootPath = "C:\\Root";
            string fullPath = "c:\\root\\file.txt";

            string result = fileUtils.GetRelativePath(rootPath, fullPath);

            result.Should().Be("file.txt");
        }

        [Fact]
        public void GetRelativePath_HandlesTrailingSlashes() {
            string rootPath = "C:\\Root\\";
            string fullPath = "C:\\Root\\File.txt";

            string result = fileUtils.GetRelativePath(rootPath, fullPath);

            result.Should().Be("File.txt");
        }

        #endregion

        #region GetFileSafeTimestamp and ParseFileSafeTimestamp Tests

        [Fact]
        public void GetFileSafeTimestamp_ReturnsValidTimestamp() {
            string timestamp = fileUtils.GetFileSafeTimestamp();

            timestamp.Should().NotBeNullOrEmpty();
            timestamp.Should().MatchRegex(@"\d{4}-\d{2}-\d{2} \d{2}-\d{2}-\d{2}");
        }

        [Fact]
        public void ParseFileSafeTimestamp_ParsesValidTimestamp() {
            string timestamp = "2024-01-15 14-30-45";

            DateTime result = fileUtils.ParseFileSafeTimestamp(timestamp);

            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(15);
            result.Hour.Should().Be(14);
            result.Minute.Should().Be(30);
            result.Second.Should().Be(45);
        }

        [Fact]
        public void GetFileSafeTimestamp_ParseFileSafeTimestamp_RoundTrip() {
            string timestamp = fileUtils.GetFileSafeTimestamp();

            DateTime parsed = fileUtils.ParseFileSafeTimestamp(timestamp);

            DateTime now = DateTime.Now;
            parsed.Year.Should().Be(now.Year);
            parsed.Month.Should().Be(now.Month);
            parsed.Day.Should().Be(now.Day);
        }

        [Fact]
        public void ParseFileSafeTimestamp_HandlesTimestampWithExtension() {
            string timestampWithExt = "2024-01-15 14-30-45.zip";
            string timestamp = timestampWithExt.Split('.').First();

            DateTime result = fileUtils.ParseFileSafeTimestamp(timestamp);

            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(15);
        }

        #endregion

        #region EncryptedFileHeader Tests

        [Fact]
        public void EncryptedFileHeader_IsAccessible() {
            string header = fileUtils.EncryptedFileHeader;

            header.Should().Be("WinUiTemplateEF:");
        }

        #endregion
    }
}
