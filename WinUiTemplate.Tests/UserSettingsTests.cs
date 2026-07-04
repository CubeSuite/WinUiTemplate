using FluentAssertions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUiTemplate.Core.Stores;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class UserSettingsTests : IDisposable
    {
        // Services & Stores
        private readonly Mock<IProgramData> mockProgramData;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IFileUtils> mockFileUtils;
        private readonly Mock<ILoggerService> mockLogger;
        private readonly Mock<INotificationService> mockNotificationService;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        
        // Fields
        private readonly string settingsFilePath;
        private static Color red = new Color { A = 255, R = 255, G = 0, B = 0 };
        private static Color blue = new Color { A = 255, R = 0, G = 0, B = 255 };

        // Constructors

        public UserSettingsTests()
        {
            settingsFilePath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");

            mockProgramData = new Mock<IProgramData>();
            mockFilePaths = new Mock<IFilePaths>();
            mockFileUtils = new Mock<IFileUtils>();
            mockLogger = new Mock<ILoggerService>();
            mockNotificationService = new Mock<INotificationService>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockFilePaths.Setup(x => x.SettingsFile).Returns(settingsFilePath);
            mockProgramData.Setup(x => x.FilePaths).Returns(mockFilePaths.Object);
            mockProgramData.Setup(x => x.ProgramName).Returns("TestProgram");

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IProgramData)))
                .Returns(mockProgramData.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(mockFileUtils.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(mockLogger.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(INotificationService)))
                .Returns(mockNotificationService.Object);

            mockLogger.Setup(x => x.LogInfo(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<bool>()));
            mockLogger.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<bool>()));
        }

        public void Dispose()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    File.Delete(settingsFilePath);
                }
                catch { }
            }
        }

        // Helper Methods

        private UserSettings CreateUserSettings()
        {
            return new UserSettings(mockServiceProvider.Object);
        }

        private void SetupSuccessfulFileRead(string json)
        {
            mockFileUtils
                .Setup(x => x.TryReadFileAsync(settingsFilePath))
                .ReturnsAsync(new FileReadResult(true, "", json));
        }

        private void SetupSuccessfulFileWrite()
        {
            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(new FileWriteResult(true, ""));

            // Also setup without the optional parameter in case Moq matches differently
            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .ReturnsAsync(new FileWriteResult(true, ""));
        }

        private void SetupFailedFileRead(string errorMessage)
        {
            mockFileUtils
                .Setup(x => x.TryReadFileAsync(settingsFilePath))
                .ReturnsAsync(new FileReadResult(false, errorMessage, null));
        }

        private void SetupFailedFileWrite(string errorMessage)
        {
            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(new FileWriteResult(false, errorMessage));

            // Also setup without the optional parameter in case Moq matches differently
            mockFileUtils
                .Setup(x => x.TryWriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .ReturnsAsync(new FileWriteResult(false, errorMessage));
        }

        // Tests

        #region Property Default Value Tests

        [Fact]
        public void AllProperties_HaveCorrectDefaultValues()
        {
            UserSettings settings = CreateUserSettings();

            settings.Loaded.Should().BeFalse();
            settings.IsFirstLaunch.Should().BeTrue();
            settings.LogDebugMessages.Should().BeFalse();
            settings.MaxLogs.Should().Be(5);
            settings.Theme.Should().Be(ThemeOption.MatchWindows);
            settings.Backdrop.Should().Be(BackdropOption.AcrylicBase);
            settings.AccentSource.Should().Be(AccentSourceOption.MatchWindows);
            settings.CustomAccentColour.Should().Be(new UISettings().GetColorValue(UIColorType.AccentLight2));
            settings.WindowTintSource.Should().Be(WindowTintSourceOption.None);
            settings.CustomWindowTintColour.Should().Be(new UISettings().GetColorValue(UIColorType.AccentLight2));
            settings.WindowTintOpacity.Should().Be(0.5);
            settings.RememberLayout.Should().BeTrue();
            settings.OpenMaximised.Should().BeFalse();
            settings.DefaultWidth.Should().Be(1600);
            settings.DefaultHeight.Should().Be(900);
            settings.BackupsFolder.Should().BeEmpty();
            settings.MaxBackups.Should().Be(5);
            settings.AutomaticBackups.Should().BeTrue();
            settings.ApiTimeout.Should().Be(10);
            settings.ApiMaxRetries.Should().Be(3);
            settings.DatabaseHost.Should().Be("localhost");
            settings.DatabasePort.Should().Be(5432);
            settings.DatabaseName.Should().BeEmpty();
            settings.DatabaseUsername.Should().BeEmpty();
            settings.DatabasePassword.Should().BeEmpty();
            settings.DatabaseConnectionTimeout.Should().Be(30);
            settings.SearchCaseSensitive.Should().BeFalse();
            settings.SearchSplitQuery.Should().BeTrue();
        }

        [Fact]
        public async Task AllProperties_HaveCorrectDefaultValue_AfterLoadFromEmptyJson() {
            UserSettings settings = CreateUserSettings();
            string json = "{}";
            SetupSuccessfulFileRead(json);
            await settings.Load();

            settings.Loaded.Should().BeFalse();
            settings.IsFirstLaunch.Should().BeTrue();
            settings.LogDebugMessages.Should().BeFalse();
            settings.MaxLogs.Should().Be(5);
            settings.Theme.Should().Be(ThemeOption.MatchWindows);
            settings.Backdrop.Should().Be(BackdropOption.AcrylicBase);
            settings.AccentSource.Should().Be(AccentSourceOption.MatchWindows);
            settings.CustomAccentColour.Should().Be(new UISettings().GetColorValue(UIColorType.AccentLight2));
            settings.WindowTintSource.Should().Be(WindowTintSourceOption.None);
            settings.CustomWindowTintColour.Should().Be(new UISettings().GetColorValue(UIColorType.AccentLight2));
            settings.WindowTintOpacity.Should().Be(0.5);
            settings.RememberLayout.Should().BeTrue();
            settings.OpenMaximised.Should().BeFalse();
            settings.DefaultWidth.Should().Be(1600);
            settings.DefaultHeight.Should().Be(900);
            settings.BackupsFolder.Should().BeEmpty();
            settings.MaxBackups.Should().Be(5);
            settings.AutomaticBackups.Should().BeTrue();
            settings.ApiTimeout.Should().Be(10);
            settings.ApiMaxRetries.Should().Be(3);
            settings.DatabaseHost.Should().Be("localhost");
            settings.DatabasePort.Should().Be(5432);
            settings.DatabaseName.Should().BeEmpty();
            settings.DatabaseUsername.Should().BeEmpty();
            settings.DatabasePassword.Should().BeEmpty();
            settings.DatabaseConnectionTimeout.Should().Be(30);
            settings.SearchCaseSensitive.Should().BeFalse();
            settings.SearchSplitQuery.Should().BeTrue();
        }

        #endregion

        #region Property Setter Tests

        [Fact]
        public void AllProperties_CanBeSet()
        {
            UserSettings settings = CreateUserSettings();

            settings.IsFirstLaunch = false;
            settings.LogDebugMessages = true;
            settings.MaxLogs = 10;
            settings.Theme = ThemeOption.Light;
            settings.Backdrop = BackdropOption.Mica;
            settings.AccentSource = AccentSourceOption.Custom;
            settings.CustomAccentColour = red;
            settings.WindowTintSource = WindowTintSourceOption.Custom;
            settings.CustomWindowTintColour = blue;
            settings.WindowTintOpacity = 0.75;
            settings.RememberLayout = false;
            settings.OpenMaximised = true;
            settings.DefaultWidth = 1920;
            settings.DefaultHeight = 1080;
            settings.BackupsFolder = "C:\\Backups";
            settings.MaxBackups = 10;
            settings.AutomaticBackups = false;
            settings.ApiTimeout = 30;
            settings.ApiMaxRetries = 5;
            settings.DatabaseHost = "db.example.com";
            settings.DatabasePort = 5433;
            settings.DatabaseName = "mydb";
            settings.DatabaseUsername = "admin";
            settings.DatabasePassword = "secret123";
            settings.DatabaseConnectionTimeout = 60;
            settings.SearchCaseSensitive = true;
            settings.SearchSplitQuery = false;

            settings.IsFirstLaunch.Should().BeFalse();
            settings.LogDebugMessages.Should().BeTrue();
            settings.MaxLogs.Should().Be(10);
            settings.Theme.Should().Be(ThemeOption.Light);
            settings.Backdrop.Should().Be(BackdropOption.Mica);
            settings.AccentSource.Should().Be(AccentSourceOption.Custom);
            settings.CustomAccentColour.Should().Be(red);
            settings.WindowTintSource.Should().Be(WindowTintSourceOption.Custom);
            settings.CustomWindowTintColour.Should().Be(blue);
            settings.WindowTintOpacity.Should().Be(0.75);
            settings.RememberLayout.Should().BeFalse();
            settings.OpenMaximised.Should().BeTrue();
            settings.DefaultWidth.Should().Be(1920);
            settings.DefaultHeight.Should().Be(1080);
            settings.BackupsFolder.Should().Be("C:\\Backups");
            settings.MaxBackups.Should().Be(10);
            settings.AutomaticBackups.Should().BeFalse();
            settings.ApiTimeout.Should().Be(30);
            settings.ApiMaxRetries.Should().Be(5);
            settings.DatabaseHost.Should().Be("db.example.com");
            settings.DatabasePort.Should().Be(5433);
            settings.DatabaseName.Should().Be("mydb");
            settings.DatabaseUsername.Should().Be("admin");
            settings.DatabasePassword.Should().Be("secret123");
            settings.DatabaseConnectionTimeout.Should().Be(60);
            settings.SearchCaseSensitive.Should().BeTrue();
            settings.SearchSplitQuery.Should().BeFalse();
        }

        #endregion

        #region SettingChanged Event Tests

        [Fact]
        public void Property_RaisesSettingChangedEvent_WhenValueChanges()
        {
            UserSettings settings = CreateUserSettings();
            string? changedPropertyName = null;
            settings.SettingChanged += (name) => changedPropertyName = name;

            settings.LogDebugMessages = true;

            changedPropertyName.Should().Be(nameof(settings.LogDebugMessages));
        }

        [Fact]
        public void Property_DoesNotRaiseSettingChangedEvent_WhenValueIsSame()
        {
            UserSettings settings = CreateUserSettings();
            int eventCount = 0;
            settings.SettingChanged += (name) => eventCount++;

            settings.SearchSplitQuery = true;

            eventCount.Should().Be(0);
        }

        #endregion

        #region Database Settings Tests

        [Fact]
        public void DatabaseSettings_CanBeSetAndRetrieved()
        {
            UserSettings settings = CreateUserSettings();

            settings.DatabaseHost = "postgres.example.com";
            settings.DatabasePort = 5433;
            settings.DatabaseName = "production_db";
            settings.DatabaseUsername = "dbadmin";
            settings.DatabasePassword = "P@ssw0rd!";
            settings.DatabaseConnectionTimeout = 45;

            settings.DatabaseHost.Should().Be("postgres.example.com");
            settings.DatabasePort.Should().Be(5433);
            settings.DatabaseName.Should().Be("production_db");
            settings.DatabaseUsername.Should().Be("dbadmin");
            settings.DatabasePassword.Should().Be("P@ssw0rd!");
            settings.DatabaseConnectionTimeout.Should().Be(45);
        }

        #endregion

        #region Load Tests

        [Fact]
        public async Task Load_SetsLoadedToTrue_WhenFileDoesNotExist()
        {
            UserSettings settings = CreateUserSettings();
            SetupFailedFileRead("File not found");

            await settings.Load();

            settings.Loaded.Should().BeTrue();
        }

        [Fact]
        public async Task Load_LoadsSettingsFromFile_WhenFileExists()
        {
            UserSettings settings = CreateUserSettings();
            string json = @"{
                ""IsFirstLaunch"": false,
                ""LogDebugMessages"": true,
                ""MaxLogs"": 15,
                ""Theme"": 1,
                ""Backdrop"": 1,
                ""AccentSource"": 1,
                ""CustomAccentColour"": {""A"":255,""R"":255,""G"":0,""B"":0},
                ""WindowTintSource"": 1,
                ""CustomWindowTintColour"": {""A"":255,""R"":0,""G"":0,""B"":255},
                ""WindowTintOpacity"": 0.8,
                ""RememberLayout"": false,
                ""OpenMaximised"": true,
                ""DefaultWidth"": 1920,
                ""DefaultHeight"": 1080,
                ""BackupsFolder"": ""C:\\Backups"",
                ""MaxBackups"": 10,
                ""AutomatedBackups"": false,
                ""ApiTimeout"": 30,
                ""ApiMaxRetries"": 5,
                ""DatabaseHost"": ""db.example.com"",
                ""DatabasePort"": 5433,
                ""DatabaseName"": ""testdb"",
                ""DatabaseUsername"": ""testuser"",
                ""DatabasePassword"": ""testpass"",
                ""DatabaseConnectionTimeout"": 60,
                ""SearchCaseSensitive"": true,
                ""SearchSplitQuery"": false
            }";
            SetupSuccessfulFileRead(json);

            await settings.Load();

            settings.Loaded.Should().BeTrue();
            settings.IsFirstLaunch.Should().BeFalse();
            settings.LogDebugMessages.Should().BeTrue();
            settings.MaxLogs.Should().Be(15);
            settings.Theme.Should().Be(ThemeOption.Dark);
            settings.Backdrop.Should().Be(BackdropOption.MicaAlt);
            settings.AccentSource.Should().Be(AccentSourceOption.Custom);
            settings.CustomAccentColour.Should().Be(red);
            settings.WindowTintSource.Should().Be(WindowTintSourceOption.Custom);
            settings.CustomWindowTintColour.Should().Be(blue);
            settings.WindowTintOpacity.Should().Be(0.8);
            settings.RememberLayout.Should().BeFalse();
            settings.OpenMaximised.Should().BeTrue();
            settings.DefaultWidth.Should().Be(1920);
            settings.DefaultHeight.Should().Be(1080);
            settings.BackupsFolder.Should().Be("C:\\Backups");
            settings.MaxBackups.Should().Be(10);
            settings.AutomaticBackups.Should().BeFalse();
            settings.ApiTimeout.Should().Be(30);
            settings.ApiMaxRetries.Should().Be(5);
            settings.DatabaseHost.Should().Be("db.example.com");
            settings.DatabasePort.Should().Be(5433);
            settings.DatabaseName.Should().Be("testdb");
            settings.DatabaseUsername.Should().Be("testuser");
            settings.DatabasePassword.Should().Be("testpass");
            settings.DatabaseConnectionTimeout.Should().Be(60);
            settings.SearchCaseSensitive.Should().BeTrue();
            settings.SearchSplitQuery.Should().BeFalse();
        }

        [Fact]
        public async Task Load_RaisesSettingsLoadedEvent_WhenSuccessful()
        {
            UserSettings settings = CreateUserSettings();
            bool eventRaised = false;
            settings.SettingsLoaded += () => eventRaised = true;
            SetupSuccessfulFileRead("{}");

            await settings.Load();

            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task Load_LogsError_WhenFileReadFails()
        {
            UserSettings settings = CreateUserSettings();
            SetupFailedFileRead("Read error");

            await settings.Load();

            mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Load_ShowsNotification_WhenFileExistsButCannotBeRead()
        {
            UserSettings settings = CreateUserSettings();
            SetupFailedFileRead("Read error");
            File.WriteAllText(settingsFilePath, "{}");

            await settings.Load();

            mockNotificationService.Verify(
                x => x.Notify(InfoBarSeverity.Error, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Load_SetsLoadedToFalse_WhenFileExistsButCannotBeRead()
        {
            UserSettings settings = CreateUserSettings();
            SetupFailedFileRead("Read error");
            File.WriteAllText(settingsFilePath, "{}");

            await settings.Load();

            settings.Loaded.Should().BeFalse();
        }

        #endregion

        #region Property Change When Loaded Tests

        [Fact]
        public async Task PropertyChange_DoesNotThrow_WhenLoaded()
        {
            UserSettings settings = CreateUserSettings();
            SetupSuccessfulFileRead("{}");
            SetupSuccessfulFileWrite();
            await settings.Load();

            Action act = () => settings.LogDebugMessages = true;

            act.Should().NotThrow();
        }

        [Fact]
        public void PropertyChange_DoesNotThrow_WhenNotLoaded()
        {
            UserSettings settings = CreateUserSettings();

            Action act = () => settings.LogDebugMessages = true;

            act.Should().NotThrow();
        }

        [Fact]
        public async Task PropertyChanges_WorkCorrectly_AfterLoad()
        {
            UserSettings settings = CreateUserSettings();
            SetupSuccessfulFileRead("{}");
            await settings.Load();

            settings.MaxLogs = 20;
            settings.LogDebugMessages = true;
            settings.BackupsFolder = "C:\\TestBackups";

            settings.MaxLogs.Should().Be(20);
            settings.LogDebugMessages.Should().BeTrue();
            settings.BackupsFolder.Should().Be("C:\\TestBackups");
        }

        #endregion

        #region RestoreDefaults Tests

        [Fact]
        public void RestoreDefaults_ResetsLogDebugMessagesToTrue()
        {
            UserSettings settings = CreateUserSettings();
            settings.LogDebugMessages = false;

            settings.RestoreDefaults();

            settings.LogDebugMessages.Should().BeTrue();
        }

        [Fact]
        public void RestoreDefaults_ResetsMaxLogsTo5()
        {
            UserSettings settings = CreateUserSettings();
            settings.MaxLogs = 20;

            settings.RestoreDefaults();

            settings.MaxLogs.Should().Be(5);
        }

        [Fact]
        public void RestoreDefaults_ResetsOpenMaximisedToFalse()
        {
            UserSettings settings = CreateUserSettings();
            settings.OpenMaximised = true;

            settings.RestoreDefaults();

            settings.OpenMaximised.Should().BeFalse();
        }

        [Fact]
        public void RestoreDefaults_ResetsMaxBackupsTo5()
        {
            UserSettings settings = CreateUserSettings();
            settings.MaxBackups = 20;

            settings.RestoreDefaults();

            settings.MaxBackups.Should().Be(5);
        }

        [Fact]
        public void RestoreDefaults_DoesNotResetBackupsFolder()
        {
            UserSettings settings = CreateUserSettings();
            settings.BackupsFolder = "C:\\Backups";

            settings.RestoreDefaults();

            settings.BackupsFolder.Should().Be("C:\\Backups");
        }

        [Fact]
        public void RestoreDefaults_DoesNotResetAutomaticBackups()
        {
            UserSettings settings = CreateUserSettings();
            settings.AutomaticBackups = false;

            settings.RestoreDefaults();

            settings.AutomaticBackups.Should().BeFalse();
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void UserSettings_CanBeCreated_WithValidConfiguration()
        {
            Action act = () => new UserSettings(mockServiceProvider.Object);

            act.Should().NotThrow();
        }

        #endregion
    }
}
