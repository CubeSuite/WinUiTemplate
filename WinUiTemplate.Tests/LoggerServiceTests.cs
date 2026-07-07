using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Services;
using WinUiTemplate.Core.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class LoggerServiceTests : IDisposable
    {
        // Services & Stores
        private readonly Mock<IProgramData> mockProgramData;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IFileUtils> mockFileUtils;
        private readonly Mock<IUserSettings> mockUserSettings;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly StorageFolder tempFolder;
        private readonly string logsFolder;
        private readonly List<LoggerService> loggerInstances = new List<LoggerService>();

        // Constructors

        public LoggerServiceTests() {
            tempFolder = TestUtils.GetTempFolder().Result;
            logsFolder = Path.Combine(tempFolder.Path, "Logs");
            Directory.CreateDirectory(logsFolder);

            mockProgramData = new Mock<IProgramData>();
            mockFilePaths = new Mock<IFilePaths>();
            mockFileUtils = new Mock<IFileUtils>();
            mockUserSettings = new Mock<IUserSettings>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockFilePaths.Setup(x => x.LogsFolder).Returns(logsFolder);
            mockFilePaths.Setup(x => x.RootFolder).Returns(tempFolder.Path);
            mockProgramData.Setup(x => x.FilePaths).Returns(mockFilePaths.Object);
            mockProgramData.Setup(x => x.IsDebugBuild).Returns(false);

            mockUserSettings.Setup(x => x.MaxLogs).Returns(10);

            mockFileUtils
                .Setup(x => x.GetFileSafeTimestamp())
                .Returns("2024-01-15 10-30-00");

            mockFileUtils
                .Setup(x => x.TryGetAllFilesAsync(It.IsAny<string>(), default))
                .ReturnsAsync(new FilesResult(true, "", System.Array.Empty<StorageFile>()));

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IProgramData)))
                .Returns(mockProgramData.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(mockFileUtils.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);
        }

        public void Dispose() {
            foreach (LoggerService logger in loggerInstances) {
                try {
                    logger.Dispose();
                }
                catch { }
            }

            TestUtils.CleanupTempFolder(tempFolder).Wait();
        }

        // Helper Methods

        private LoggerService CreateLogger() {
            LoggerService logger = new LoggerService(mockServiceProvider.Object);
            loggerInstances.Add(logger);
            return logger;
        }

        // Tests

        #region Logging Methods Tests

        [Fact]
        public void LogDebug_AddsEntryToLogEntries() {
            LoggerService logger = CreateLogger();

            logger.LogDebug("Debug message");

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Level.Should().Be("Debug");
            logger.LogEntries[0].Message.Should().Be("Debug message");
        }

        [Fact]
        public void LogInfo_AddsEntryToLogEntries() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("Info message");

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Level.Should().Be("Info");
            logger.LogEntries[0].Message.Should().Be("Info message");
        }

        [Fact]
        public void LogWarning_AddsEntryToLogEntries() {
            LoggerService logger = CreateLogger();

            logger.LogWarning("Warning message");

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Level.Should().Be("Warn");
            logger.LogEntries[0].Message.Should().Be("Warning message");
        }

        [Fact]
        public void LogError_AddsEntryToLogEntries() {
            LoggerService logger = CreateLogger();

            logger.LogError("Error message");

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Level.Should().Be("Error");
            logger.LogEntries[0].Message.Should().Be("Error message");
        }

        [Fact]
        public void LogFatal_AddsEntryToLogEntries() {
            LoggerService logger = CreateLogger();

            logger.LogFatal("Fatal message");

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Level.Should().Be("Fatal");
            logger.LogEntries[0].Message.Should().Be("Fatal message");
        }

        [Fact]
        public void LogFatal_InvokesOnFatalEvent() {
            LoggerService logger = CreateLogger();
            bool eventInvoked = false;
            logger.OnFatal += () => eventInvoked = true;

            logger.LogFatal("Fatal error");

            eventInvoked.Should().BeTrue();
        }

        [Fact]
        public void LogMessage_WithTags_IncludesTagsInEntry() {
            LoggerService logger = CreateLogger();
            string[] tags = { "Tag1", "Tag2" };

            logger.LogInfo("Tagged message", tags);

            logger.LogEntries.Should().HaveCount(1);
            logger.LogEntries[0].Tags.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public void LogMessage_WithMultipleLevels_CreatesMultipleEntries() {
            LoggerService logger = CreateLogger();

            logger.LogDebug("Debug");
            logger.LogInfo("Info");
            logger.LogWarning("Warning");
            logger.LogError("Error");

            logger.LogEntries.Should().HaveCount(4);
            logger.LogEntries[0].Level.Should().Be("Debug");
            logger.LogEntries[1].Level.Should().Be("Info");
            logger.LogEntries[2].Level.Should().Be("Warn");
            logger.LogEntries[3].Level.Should().Be("Error");
        }

        #endregion

        #region Path Shortening Tests

        [Fact]
        public void LogMessage_ShortensRootFolderPath_WhenShortenPathsIsTrue() {
            LoggerService logger = CreateLogger();
            string messageWithPath = $"File at {tempFolder.Path}\\test.txt";

            logger.LogInfo(messageWithPath, shortenPaths: true);

            logger.LogEntries[0].Message.Should().Be("File at \\test.txt");
        }

        [Fact]
        public void LogMessage_DoesNotShortenPaths_WhenShortenPathsIsFalse() {
            LoggerService logger = CreateLogger();
            string messageWithPath = $"File at {tempFolder.Path}\\test.txt";

            logger.LogInfo(messageWithPath, shortenPaths: false);

            logger.LogEntries[0].Message.Should().Be(messageWithPath);
        }

        #endregion

        #region LogDebugToFile Property Tests

        [Fact]
        public void LogDebugToFile_ReturnsFalse_WhenNotDebugBuildAndNotSet() {
            LoggerService logger = CreateLogger();

            logger.LogDebugToFile.Should().BeFalse();
        }

        [Fact]
        public void LogDebugToFile_ReturnsTrue_WhenSet() {
            LoggerService logger = CreateLogger();

            logger.LogDebugToFile = true;

            logger.LogDebugToFile.Should().BeTrue();
        }

        [Fact]
        public void LogDebugToFile_ReturnsTrue_WhenIsDebugBuild() {
            mockProgramData.Setup(x => x.IsDebugBuild).Returns(true);
            LoggerService logger = CreateLogger();

            logger.LogDebugToFile.Should().BeTrue();
        }

        #endregion

        #region Pause and Resume Tests

        [Fact]
        public void Pause_CanBeCalledWithoutException() {
            LoggerService logger = CreateLogger();
            logger.LogInfo("Before pause");

            Action act = () => logger.Pause();

            act.Should().NotThrow();
        }

        [Fact]
        public void Resume_CanBeCalledWithoutException() {
            LoggerService logger = CreateLogger();
            logger.LogInfo("Before pause");
            logger.Pause();

            Action act = () => logger.Resume();

            act.Should().NotThrow();
        }

        [Fact]
        public void PauseThenResume_CanBeCalledMultipleTimes() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("Message 1");
            logger.Pause();
            logger.Resume();
            logger.LogInfo("Message 2");
            logger.Pause();
            logger.Resume();

            logger.LogEntries.Should().HaveCount(2);
        }

        [Fact]
        public async Task Pause_CalledConcurrentlyWithFlush_DoesNotThrow() {
            LoggerService logger = CreateLogger();
            logger.LogInfo("Init");

            Task logTask = Task.Run(() => {
                for (int i = 0; i < 100; i++) {
                    logger.LogInfo($"Concurrent {i}");
                }
            });

            Task pauseTask = Task.Run(() => logger.Pause());

            Func<Task> act = () => Task.WhenAll(logTask, pauseTask);

            await act.Should().NotThrowAsync();
        }

        #endregion

        #region File Creation Tests

        [Fact]
        public void LogMessage_CreatesLogFile_OnFirstLog() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("First message");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            File.Exists(expectedLogFile).Should().BeTrue();
        }

        [Fact]
        public async Task LogMessage_WritesToLogFile() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("Test message");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            content.Should().Contain("Test message");
        }

        [Fact]
        public async Task LogDebug_WritesToFile_WhenLogDebugToFileIsTrue() {
            LoggerService logger = CreateLogger();
            logger.LogDebugToFile = true;

            logger.LogDebug("Debug message");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            content.Should().Contain("Debug message");
        }

        [Fact]
        public async Task LogDebug_DoesNotWriteToFile_WhenLogDebugToFileIsFalse() {
            mockProgramData.Setup(x => x.IsDebugBuild).Returns(false);
            LoggerService logger = CreateLogger();
            logger.LogDebugToFile = false;

            logger.LogDebug("Debug message");
            logger.LogInfo("Info message");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            content.Should().NotContain("Debug message");
            content.Should().Contain("Info message");
        }

        [Fact]
        public async Task LogMessage_WritesMultipleMessagesToFile() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("Message 1");
            logger.LogWarning("Message 2");
            logger.LogError("Message 3");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            content.Should().Contain("Message 1");
            content.Should().Contain("Message 2");
            content.Should().Contain("Message 3");
        }

        [Fact]
        public async Task LogMessage_AfterPauseAndResume_WritesToFile() {
            LoggerService logger = CreateLogger();
            logger.LogInfo("Before pause");
            logger.Pause();

            logger.Resume();
            logger.LogInfo("After resume");
            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            content.Should().Contain("Before pause");
            content.Should().Contain("After resume");
        }

        #endregion

        #region Initialization Tests

        [Fact]
        public void LogMessage_ThrowsException_WhenLogsFolderMissing() {
            Directory.Delete(logsFolder);
            LoggerService logger = CreateLogger();

            Action act = () => logger.LogInfo("Test");

            act.Should().Throw<DirectoryNotFoundException>()
                .WithMessage($"Logs folder missing: {logsFolder}");
        }

        [Fact]
        public void LoggerService_CanBeCreated_WithValidConfiguration() {
            Action act = () => new LoggerService(mockServiceProvider.Object);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task Initialise_ConcurrentFirstLogs_DoNotThrow() {
            LoggerService logger = CreateLogger();
            int threadCount = 5;
            using Barrier barrier = new Barrier(threadCount);

            Task[] tasks = Enumerable.Range(0, threadCount)
                .Select(i => Task.Run(() => {
                    barrier.SignalAndWait();
                    logger.LogInfo($"Thread {i}");
                }))
                .ToArray();

            Func<Task> act = () => Task.WhenAll(tasks);

            await act.Should().NotThrowAsync();
            File.Exists(Path.Combine(logsFolder, "2024-01-15 10-30-00.log")).Should().BeTrue();
        }

        #endregion

        #region Lifecycle Tests

        [Fact]
        public async Task Dispose_FlushesRemainingQueuedEntries() {
            LoggerService logger = CreateLogger();

            for (int i = 0; i < 20; i++) {
                logger.LogInfo($"Message {i}");
            }

            logger.Dispose();

            string expectedLogFile = Path.Combine(logsFolder, "2024-01-15 10-30-00.log");
            string content = await File.ReadAllTextAsync(expectedLogFile);
            for (int i = 0; i < 20; i++) {
                content.Should().Contain($"Message {i}");
            }
        }

        #endregion

        #region ObservableCollection Tests

        [Fact]
        public void LogEntries_IsInitiallyEmpty() {
            LoggerService logger = CreateLogger();

            logger.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public void LogEntries_PreservesOrder() {
            LoggerService logger = CreateLogger();

            logger.LogInfo("First");
            logger.LogWarning("Second");
            logger.LogError("Third");

            logger.LogEntries[0].Message.Should().Be("First");
            logger.LogEntries[1].Message.Should().Be("Second");
            logger.LogEntries[2].Message.Should().Be("Third");
        }

        #endregion
    }
}

