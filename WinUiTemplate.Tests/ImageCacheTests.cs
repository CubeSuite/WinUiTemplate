using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores;
using WinUiTemplate.Core.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class ImageCacheTests : IDisposable
    {
        private readonly Mock<INotificationService> notificationService = new();
        private readonly Mock<IEncryptionService> encryptionService = new();
        private readonly Mock<IDialogService> dialogService = new();
        private readonly Mock<IUserSettings> userSettings = new();
        private readonly Mock<IProgramData> programData = new();
        private readonly Mock<ILoggerService> logger = new();
        private readonly Mock<IFileUtils> fileUtils = new();
        private readonly Mock<IServiceProvider> serviceProvider = new();

        private readonly string testRoot;
        private readonly string cacheFolder;
        private readonly string saveFile;

        public ImageCacheTests() {
            // Debug.Assert on failure paths would otherwise pop a dialog and hang the run
            Trace.Listeners.Clear();

            testRoot = Path.Combine(Path.GetTempPath(), "ImageCacheTests_" + Guid.NewGuid().ToString("N"));
            cacheFolder = Path.Combine(testRoot, "cache");
            Directory.CreateDirectory(cacheFolder);
            saveFile = Path.Combine(testRoot, "imagecache.json");

            Mock<IFilePaths> mockFilePaths = new Mock<IFilePaths>();
            mockFilePaths.Setup(paths => paths.ImageCacheFolder).Returns(cacheFolder);
            mockFilePaths.Setup(paths => paths.ImageCacheSaveFile).Returns(saveFile);  

            programData.SetupGet(p => p.FilePaths).Returns(mockFilePaths.Object);
            programData.SetupGet(p => p.EncryptionLevel).Returns(EncryptionLevel.Settings);
            userSettings.SetupGet(u => u.ImageCacheEnabled).Returns(true);
            userSettings.SetupGet(u => u.ImageCacheWarnSizeGb).Returns(10);
            dialogService.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            fileUtils.Setup(f => f.TryReadFileAsync(It.IsAny<string>())).ReturnsAsync(ReadFail());
            fileUtils.Setup(f => f.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(WriteOk());

            RegisterService(notificationService.Object);
            RegisterService(encryptionService.Object);
            RegisterService(dialogService.Object);
            RegisterService(userSettings.Object);
            RegisterService(programData.Object);
            RegisterService(logger.Object);
            RegisterService(fileUtils.Object);
        }

        public void Dispose() {
            try { Directory.Delete(testRoot, recursive: true); } catch { }
        }

        // Helpers

        private void RegisterService<T>(T instance) where T : class {
            serviceProvider.Setup(sp => sp.GetService(typeof(T))).Returns(instance);
        }

        private ImageCache CreateCache() => new ImageCache(serviceProvider.Object);

        private async Task<ImageCache> CreateLoadedCache(Dictionary<string, string>? savedEntries = null) {
            if (savedEntries != null) {
                string json = JsonConvert.SerializeObject(savedEntries);
                fileUtils.Setup(f => f.TryReadFileAsync(saveFile)).ReturnsAsync(ReadOk(json));
            }

            ImageCache cache = CreateCache();
            await cache.Load();
            return cache;
        }

        private string CreateCachedFile(long sizeBytes) {
            string path = Path.Combine(cacheFolder, Guid.NewGuid().ToString("N") + ".png");
            using (FileStream stream = File.Create(path)) {
                stream.SetLength(sizeBytes);
            }
            return path;
        }

        private static FileReadResult ReadOk(string content) => new FileReadResult(true, null, content);
        private static FileReadResult ReadFail() => new FileReadResult(false, "Mock error", null);
        private static FileWriteResult WriteOk() => new FileWriteResult(true, null);
        private static FilesResult FilesOk(List<StorageFile> files) => new FilesResult(true, null, files);
        private static FilesResult FilesFail(string error) => new FilesResult(false, error, null);

        // Load

        [Fact]
        public async Task Load_WithMissingSaveFile_CompletesWithEmptyCache() {
            ImageCache cache = await CreateLoadedCache();

            cache.CacheSize.Should().Be("0 B");
            logger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Load_WithUnreadableExistingSaveFile_LogsError() {
            File.WriteAllText(saveFile, "unreadable");
            fileUtils.Setup(f => f.TryReadFileAsync(saveFile)).ReturnsAsync(ReadFail());

            await CreateLoadedCache();

            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Failed to load image cache save file"))), Times.Once);
        }

        [Fact]
        public async Task Load_WithContentThatDeserialisesToNull_LogsError() {
            fileUtils.Setup(f => f.TryReadFileAsync(saveFile)).ReturnsAsync(ReadOk("null"));

            ImageCache cache = CreateCache();
            await cache.Load();

            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("de-serialise"))), Times.Once);
        }

        [Fact]
        public async Task Load_WithCorruptJson_SwallowsExceptionAndStillFinishesLoading() {
            fileUtils.Setup(f => f.TryReadFileAsync(saveFile)).ReturnsAsync(ReadOk("{ not valid json"));

            ImageCache cache = CreateCache();
            Func<Task> act = () => cache.Load();

            await act.Should().NotThrowAsync();

            // GetImage returns immediately instead of waiting out the 30s load timeout,
            // which proves the loaded flag was set despite the bad file
            (await cache.GetImage("relative/path.png")).Should().BeNull();
        }

        [Fact]
        public async Task Load_WithSavedEntries_RestoresCache() {
            string cachedFile = CreateCachedFile(512);
            ImageCache cache = await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = cachedFile
            });

            cache.CacheSize.Should().Be("512 B");
            logger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("Finished loading image cache"))), Times.Once);
        }

        [Fact]
        public async Task Load_IgnoresEntriesWhoseCachedFileIsMissing_WhenMeasuringSize() {
            string missingFile = Path.Combine(cacheFolder, "deleted.png");
            ImageCache cache = await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = missingFile
            });

            cache.CacheSize.Should().Be("0 B");
        }

        [Fact]
        public async Task Load_WhenCacheExceedsWarnLimit_ShowsWarningNotification() {
            userSettings.SetupGet(u => u.ImageCacheWarnSizeGb).Returns(0);
            string cachedFile = CreateCachedFile(1024);

            await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = cachedFile
            });

            notificationService.Verify(n => n.Notify(
                InfoBarSeverity.Warning, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Action>()
            ), Times.Once);
        }

        [Fact]
        public async Task Load_WhenCacheWithinWarnLimit_DoesNotNotify() {
            string cachedFile = CreateCachedFile(1024);

            await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = cachedFile
            });

            notificationService.VerifyNoOtherCalls();
        }

        // CacheSize formatting

        [Theory]
        [InlineData(512, "512 B")]
        [InlineData(2_048, "2.00 KB")]
        [InlineData(1_536, "1.50 KB")]
        [InlineData(3_145_728, "3.00 MB")]
        public async Task CacheSize_FormatsTotalSizeForDisplay(long sizeBytes, string expected) {
            string cachedFile = CreateCachedFile(sizeBytes);
            ImageCache cache = await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = cachedFile
            });

            cache.CacheSize.Should().Be(expected);
        }

        [Fact]
        public async Task CacheSize_SumsAllCachedFiles() {
            ImageCache cache = await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = CreateCachedFile(300),
                ["https://example.com/b.png"] = CreateCachedFile(200)
            });

            cache.CacheSize.Should().Be("500 B");
        }

        // GetImage

        [Fact]
        public async Task GetImage_WithRelativePath_ReturnsNullAndLogsError() {
            ImageCache cache = await CreateLoadedCache();

            var image = await cache.GetImage(@"images\icon.png");

            image.Should().BeNull();
            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unknown or relative"))), Times.Once);
        }

        [Fact]
        public async Task GetImage_WithUnsupportedScheme_ReturnsNullAndLogsError() {
            ImageCache cache = await CreateLoadedCache();

            var image = await cache.GetImage("mailto:someone@example.com");

            image.Should().BeNull();
            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unknown or relative"))), Times.Once);
        }

        [Fact]
        public async Task GetImage_WithMissingLocalFile_LogsCacheFailure() {
            ImageCache cache = await CreateLoadedCache();

            // Caching fails because the file doesn't exist, then GetImage falls back to
            // loading the original path directly, which throws - current behaviour
            Func<Task> act = () => cache.GetImage("file:///C:/definitely/not/here.png");

            await act.Should().ThrowAsync<Exception>();
            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("file does not exist"))), Times.Once);
        }

        // ClearCache

        [Fact]
        public async Task ClearCache_WhenUserDeclines_ReturnsDeclinedWithoutTouchingFiles() {
            dialogService.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            ImageCache cache = CreateCache();

            OperationResult result = await cache.ClearCache();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Declined");
            fileUtils.Verify(f => f.TryGetAllFilesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ClearCache_WhenFileListingFails_ReturnsFailureAndLogsError() {
            fileUtils.Setup(f => f.TryGetAllFilesAsync(cacheFolder)).ReturnsAsync(FilesFail("access denied"));
            ImageCache cache = CreateCache();

            OperationResult result = await cache.ClearCache();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("access denied");
            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Failed to clear image cache"))), Times.Once);
        }

        [Fact]
        public async Task ClearCache_WithEmptyFolder_SucceedsAndSavesEmptyCache() {
            fileUtils.Setup(f => f.TryGetAllFilesAsync(cacheFolder)).ReturnsAsync(FilesOk(new List<StorageFile>()));
            ImageCache cache = CreateCache();

            OperationResult result = await cache.ClearCache();

            result.Success.Should().BeTrue();
            fileUtils.Verify(f => f.TryWriteFileAsync(saveFile, "{}"), Times.Once);
        }

        [Fact]
        public async Task ClearCache_RemovesPreviouslyLoadedEntries() {
            string cachedFile = CreateCachedFile(256);
            fileUtils.Setup(f => f.TryGetAllFilesAsync(cacheFolder)).ReturnsAsync(FilesOk(new List<StorageFile>()));
            ImageCache cache = await CreateLoadedCache(new Dictionary<string, string> {
                ["https://example.com/a.png"] = cachedFile
            });

            OperationResult result = await cache.ClearCache();

            result.Success.Should().BeTrue();
            fileUtils.Verify(f => f.TryWriteFileAsync(saveFile, "{}"), Times.Once);
        }

        // Save (exercised through ClearCache)

        [Fact]
        public async Task Save_WhenWriteFails_LogsErrorInsteadOfThrowing() {
            fileUtils.Setup(f => f.TryGetAllFilesAsync(cacheFolder)).ReturnsAsync(FilesOk(new List<StorageFile>()));
            fileUtils.Setup(f => f.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(new FileWriteResult(false, null));
            ImageCache cache = CreateCache();

            Func<Task> act = () => cache.ClearCache();

            await act.Should().NotThrowAsync();
            logger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Failed to save image cache data"))), Times.Once);
        }
    }
}
