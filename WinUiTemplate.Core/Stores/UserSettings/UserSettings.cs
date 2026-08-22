using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUiTemplate.Core.MVVM.Models;
using WinUiTemplate.Core.Services;
using WinUiTemplate.Core.Services.Interfaces;   
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class UserSettings : IUserSettings
    {
        /* How to add a setting:
            * Create a private member and give it a default value
            * Create a public property for it
            * Add a property with the same name to IUserSettings
            * Add a member to the SettingsDTO object
            * Add that member to the ToDTO() and LoadFromDTO() functions
            * Add a ViewModel to MVVM/Pages/SettingsPage/SettingsPageViewModel
            * If the theme should be reapplied after changing the setting, add it to ThemeService.appearanceSettings
        */

        // Services & Stores
        private readonly INotificationService notificationService;
        private readonly IDialogService? dialogService;
        private readonly IProgramData programData;
        private readonly ILoggerService logger;
        private readonly IFileUtils fileUtils;
        private readonly ILanguageService lang;

        // Fields
        private readonly object saveLock = new object();
        private readonly SemaphoreSlim saveSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? tokenSource;

        private const int saveDebounceDelayMs = 200;

        private record SettingsDTO(
            // Hidden
            bool? IsFirstLaunch,
            bool? LogDebugMessages,

            // Logging  
            int? MaxLogs,

            // Appearance
            LanguageOption? Language,
            bool? EasyLanguageSwitching,
            ThemeOption? Theme,
            BackdropOption? Backdrop,
            AccentSourceOption? AccentSource,
            Color? CustomAccentColour,
            WindowTintSourceOption? WindowTintSource,
            Color? SolidWindowTintColour,
            GradientOption? GradientPreset,
            GradientStep? GradientStart,
            GradientStep? GradientEnd,
            double? WindowTintOpacity,

            // Layout
            bool? RememberLayout,
            bool? OpenMaximised,
            int? DefaultWidth,
            int? DefaultHeight,

            // Backups
            string? BackupsFolder,
            int? MaxBackups,
            bool? AutomatedBackups,

            // API
            int? ApiTimeout,
            int? ApiMaxRetries,

            // Remote Database
            string? DatabaseHost,
            int? DatabasePort,
            string? DatabaseName,
            string? DatabaseUsername,
            string? DatabasePassword,
            int? DatabaseConnectionTimeout,
            SslMode? DatabaseSslMode,

            // Search
            bool? SearchCaseSensitive,
            bool? SearchSplitQuery,

            // Image Cache
            bool? ImageCacheEnabled,
            int? ImageCacheWarnSizeGb
        );

        #region Setting Fields

        // Hidden
        private bool _loaded = false;
        private bool _isFirstLaunch = true;

        // Logging
        private bool _logDebugMessages = false;
        private int _maxLogs = 5;

        // Appearance
        private LanguageOption _language = LanguageOption.en_GB;
        private bool _easyLanguageSwitching;
        private ThemeOption _theme = ThemeOption.MatchWindows;
        private BackdropOption _backdrop = BackdropOption.AcrylicBase;
        private AccentSourceOption _accentSource = AccentSourceOption.MatchWindows;
        private Color _customAccentColour = GetWindowsAccentColour();
        private WindowTintSourceOption _windowTintSource = WindowTintSourceOption.None;
        private Color _solidWindowTintColour = GetWindowsAccentColour();
        private GradientOption _gradientPreset = GradientOption.Custom;
        private GradientStep _gradientStart = new GradientStep();
        private GradientStep _gradientEnd = new GradientStep();
        private double _windowTintOpacity = 0.5;

        // Layout
        private bool _rememberLayout = true;
        private bool _openMaximised = false;
        private int _defaultWidth = 1600;
        private int _defaultHeight = 900;

        // Backups
        private string _backupsFolder = "";
        private int _maxBackups = 5;
        private bool _automaticBackups = true;

        // API
        private int _apiTimeout = 30;
        private int _apiMaxRetries = 3;

        // Remote Database
        private string _databaseHost = "localhost";
        private int _databasePort = 5432;
        private string _databaseName = "";
        private string _databaseUsername = "";
        private string _databasePassword = "";
        private int _databaseConnectionTimeout = 30;
        private SslMode _databaseSslMode = SslMode.Disable;

        // Search
        private bool _searchCaseSensitive = false;
        private bool _searchSplitQuery = true;

        // Image Cache
        private bool _imageCacheEnabled = true;
        private int _imageCacheWarnSizeGb = 1;

        #endregion

        #region Setting Properties

        // Hidden

        public bool Loaded {
            get => _loaded;
            private set {
                if (_loaded == value) return;
                _loaded = value;
                if (value) SettingsLoaded?.Invoke();
            }
        }

        public bool IsFirstLaunch {
            get => _isFirstLaunch;
            set => SetSetting(ref _isFirstLaunch, value);
        }

        // Logging

        public bool LogDebugMessages {
            get => _logDebugMessages;
            set => SetSetting(ref _logDebugMessages, value);
        }

        public int MaxLogs {
            get => _maxLogs;
            set => SetSetting(ref _maxLogs, value);
        }

        // Appearance

        public LanguageOption Language {
            get => _language;
            set {
                bool changed = _language != value;
                SetSetting(ref _language, value);

                if (!changed) return;
                ApplicationLanguages.PrimaryLanguageOverride = _language.ToLanguageCode();
                LanguageService.ReloadLanguage();
            }
        }
        public bool EasyLanguageSwitching {
            get => _easyLanguageSwitching;
            set => SetSetting(ref _easyLanguageSwitching, value);
        }
        public ThemeOption Theme {
            get => _theme;
            set => SetSetting(ref _theme, value);
        }

        public BackdropOption Backdrop {
            get => _backdrop;
            set => SetSetting(ref _backdrop, value);
        }

        public AccentSourceOption AccentSource {
            get => _accentSource;
            set => SetSetting(ref _accentSource, value);
        }

        public Color CustomAccentColour {
            get => _customAccentColour;
            set => SetSetting(ref _customAccentColour, value);
        }

        public WindowTintSourceOption WindowTintSource {
            get => _windowTintSource;
            set => SetSetting(ref _windowTintSource, value);
        }

        public Color CustomWindowTintColour {
            get => _customWindowTintColour;
            set => SetSetting(ref _customWindowTintColour, value);
        }

        public double WindowTintOpacity {
            get => _windowTintOpacity;
            set => SetSetting(ref _windowTintOpacity, value);
        }

        // Layout

        public bool RememberLayout {
            get => _rememberLayout;
            set => SetSetting(ref _rememberLayout, value);
        }

        public bool OpenMaximised {
            get => _openMaximised;
            set => SetSetting(ref _openMaximised, value);
        }

        public int DefaultWidth {
            get => _defaultWidth;
            set => SetSetting(ref _defaultWidth, value);
        }

        public int DefaultHeight {
            get => _defaultHeight;
            set => SetSetting(ref _defaultHeight, value);
        }

        // Backups

        public string BackupsFolder {
            get => _backupsFolder;
            set => SetSetting(ref _backupsFolder, value);
        }

        public int MaxBackups {
            get => _maxBackups;
            set => SetSetting(ref _maxBackups, value);
        }

        public bool AutomaticBackups {
            get => _automaticBackups;
            set => SetSetting(ref _automaticBackups, value);
        }

        // API

        public int ApiTimeout {
            get => _apiTimeout;
            set => SetSetting(ref _apiTimeout, value);
        }

        public int ApiMaxRetries {
            get => _apiMaxRetries;
            set => SetSetting(ref _apiMaxRetries, value);
        }

        // Remote Database

        public string DatabaseHost {
            get => _databaseHost;
            set => SetSetting(ref _databaseHost, value);
        }

        public int DatabasePort {
            get => _databasePort;
            set => SetSetting(ref _databasePort, value);
        }

        public string DatabaseName {
            get => _databaseName;
            set => SetSetting(ref _databaseName, value);
        }

        public string DatabaseUsername {
            get => _databaseUsername;
            set => SetSetting(ref _databaseUsername, value);
        }

        public string DatabasePassword {
            get => _databasePassword;
            set => SetSetting(ref _databasePassword, value);
        }

        public int DatabaseConnectionTimeout {
            get => _databaseConnectionTimeout;
            set => SetSetting(ref _databaseConnectionTimeout, value);
        }

        public SslMode DatabaseSslMode {
            get => _databaseSslMode;
            set => SetSetting(ref _databaseSslMode, value);
        }

        // Search

        public bool SearchCaseSensitive {
            get => _searchCaseSensitive;
            set => SetSetting(ref _searchCaseSensitive, value);
        }

        public bool SearchSplitQuery {
            get => _searchSplitQuery;
            set => SetSetting(ref _searchSplitQuery, value);
        }

        // Image Cache

        public bool ImageCacheEnabled {
            get => _imageCacheEnabled;
            set => SetSetting(ref _imageCacheEnabled, value);
        }

        public int ImageCacheWarnSizeGb {
            get => _imageCacheWarnSizeGb;
            set => SetSetting(ref _imageCacheWarnSizeGb, value);
        }

        #endregion

        // Constructors

        public UserSettings(IServiceProvider serviceProvider) {
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            dialogService = serviceProvider.GetService(typeof(IDialogService)) as IDialogService;
            programData = serviceProvider.GetRequiredService<IProgramData>();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
            lang = new LanguageService("UserSettings");
            _easyLanguageSwitching = programData.EasyLanguageSwitching;
        }

        // Events
        public event Action? SettingsLoaded;
        public event Action<string>? SettingChanged;

        // Public Functions

        public async Task Load() {
            try {
                FileReadResult result = await fileUtils.TryReadFileAsync(programData.FilePaths.SettingsFile);
                if (!result.Success) {
                    logger.LogError($"Failed to load Settings.json - '{result.ErrorMessage}'");
                    Loaded = true;

                    if (File.Exists(programData.FilePaths.SettingsFile)) {
                        notificationService.Notify(InfoBarSeverity.Error, $"Failed to load settings. Please close {programData.ProgramName} and contact the developer.");
                        Loaded = false;
                    }

                    return;
                }

                SettingsDTO? dto;
                try {
                    dto = JsonConvert.DeserializeObject<SettingsDTO>(result.Content ?? "{}");
                }
                catch (JsonException e) {
                    await HandleCorruptedSettingsAsync(result.Content ?? "", e.Message);
                    Loaded = true;
                    return;
                }

                if (dto == null) {
                    string error = $"Parsed Settings.json is null";
                    Debug.Assert(false, error);
                    logger.LogError(error);
                    Loaded = true;
                    return;
                }

                LoadFromDTO(dto);
                ApplicationLanguages.PrimaryLanguageOverride = Language.ToLanguageCode();
                LanguageService.ReloadLanguage();
                Loaded = true;
                logger.LogInfo("Loaded UserSettings");
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.Load failed: '{e.Message}'");
                Loaded = true;
            }
        }

        public void RestoreDefaults() {
            // Logging
            LogDebugMessages = false;
            MaxLogs = 5;

            // Appearance
            Language = LanguageOption.en_GB;
            EasyLanguageSwitching = programData.EasyLanguageSwitching;
            Theme = ThemeOption.MatchWindows;
            Backdrop = BackdropOption.AcrylicBase;
            AccentSource = AccentSourceOption.MatchWindows;
            CustomAccentColour = GetWindowsAccentColour();
            WindowTintSource = WindowTintSourceOption.None;
            CustomWindowTintColour = GetWindowsAccentColour();
            WindowTintOpacity = 0.5;

            // Layout
            RememberLayout = true;
            OpenMaximised = false;
            DefaultWidth = 1600;
            DefaultHeight = 900;

            // Backups
            MaxBackups = 5;
            // Don't restore BackupsFolder and AutomaticBackups

            // API
            ApiTimeout = 30;
            ApiMaxRetries = 3;

            // Remote Database
            DatabaseHost = "localhost";
            DatabasePort = 5432;
            DatabaseName = "";
            DatabaseUsername = "";
            DatabasePassword = "";
            DatabaseConnectionTimeout = 30;
            DatabaseSslMode = SslMode.Disable;

            // Search
            SearchCaseSensitive = false;
            SearchSplitQuery = true;

            // Image Cache
            ImageCacheEnabled = true;
            ImageCacheWarnSizeGb = 1;
        }

        public async Task ChangeLanguageAsync(LanguageOption language) {
            if (Language == language) return;

            Language = language;

            if (dialogService != null) {
                await dialogService.ShowMessage(
                    MessageType.Info,
                    lang.Get("LanguageChangedTitle"),
                    string.Format(lang.Get("LanguageChangedMessage"), programData.ProgramName)
                );
            }

            await SaveNowAsync();

            if (Environment.ProcessPath is string processPath) {
                Process.Start(processPath);
            }

            Environment.Exit(0);
        }

        public async Task SaveNowAsync() {
            try {
                lock (saveLock) {
                    tokenSource?.Cancel();
                    tokenSource = null;
                }

                await SaveAsync();
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.SaveNowAsync failed: '{e.Message}'");
            }
        }

        // Private Functions

        private async Task HandleCorruptedSettingsAsync(string corruptedContent, string parseError) {
            string error = $"Settings.json is corrupted and could not be parsed - '{parseError}'";
            logger.LogError(error);

            string corruptedFilePath = Path.Combine(programData.FilePaths.DataFolder, "SettingsCorrupted.json");

            try {
                await fileUtils.TryWriteFileAsync(corruptedFilePath, corruptedContent);
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.HandleCorruptedSettingsAsync failed to write corrupted settings backup: '{e.Message}'");
            }

            notificationService.Notify(
                InfoBarSeverity.Error,
                "Settings File Corrupted",
                $"{programData.ProgramName}'s settings file was corrupted and default settings have been loaded. A copy of the corrupted file has been saved.",
                "Show In Explorer",
                () => {
                    try {
                        Process.Start(new ProcessStartInfo() {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{corruptedFilePath}\"",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception e) {
                        Debug.Assert(false, $"UserSettings.HandleCorruptedSettingsAsync failed to open Explorer: '{e.Message}'");
                    }
                }
            );
        }

        private void SetSetting<T>(ref T field, T value, [CallerMemberName] string name = "") {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            if (Loaded) DebounceSave();
            SettingChanged?.Invoke(name);
        }

        private void DebounceSave() {
            try {
                lock (saveLock) {
                    tokenSource?.Cancel();
                    tokenSource = new CancellationTokenSource();
                    CancellationToken token = tokenSource.Token;

                    _ = Task.Run(async () => {
                        try {
                            await Task.Delay(saveDebounceDelayMs, token);
                            if (!token.IsCancellationRequested) await SaveAsync();
                        }
                        catch (TaskCanceledException) { } // Expected
                        catch (Exception e) {
                            Debug.Assert(false, $"UserSettings.DebounceSave failed: '{e.Message}'");
                        }
                    });
                }
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.DebounSave outer failed: '{e.Message}'");
            }
        }

        private async Task SaveAsync() {
            await saveSemaphore.WaitAsync();
            try {
                string json = JsonConvert.SerializeObject(ToDTO(), Formatting.Indented);
                FileWriteResult result = await fileUtils.TryWriteFileAsync(programData.FilePaths.SettingsFile, json);
                if (result.Success) {
                    logger.LogInfo("Saved UserSettings");
                }
                else {
                    string error = $"Failed to save UserSettings - '{result.ErrorMessage}'";
                    logger.LogError(error);
                    notificationService.Notify(InfoBarSeverity.Error, error);
                }
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.SaveAsync failed: '{e.Message}'");
            }
            finally {
                saveSemaphore.Release();
            }
        }

        private SettingsDTO ToDTO() => new SettingsDTO(
            // Hidden
            _isFirstLaunch,

            // Logging
            _logDebugMessages,
            _maxLogs,

            // Appearance
            _language,
            _easyLanguageSwitching,
            _theme,
            _backdrop,
            _accentSource,
            _customAccentColour,
            _windowTintSource,
            _customWindowTintColour,
            _windowTintOpacity,

            // Layout
            _rememberLayout,
            _openMaximised,
            _defaultWidth,
            _defaultHeight,
            _backupsFolder,

            // Backups
            _maxBackups,
            _automaticBackups,

            // API
            _apiTimeout,
            _apiMaxRetries,

            // Remote Database
            _databaseHost,
            _databasePort,
            _databaseName,
            _databaseUsername,
            _databasePassword,
            _databaseConnectionTimeout,
            _databaseSslMode,

            // Search
            _searchCaseSensitive,
            _searchSplitQuery,

            // Image Cache
            _imageCacheEnabled,
            _imageCacheWarnSizeGb
        );

        private void LoadFromDTO(SettingsDTO dto) {
            // Hidden
            _isFirstLaunch = dto.IsFirstLaunch ?? true;

            // Logging
            _logDebugMessages = dto.LogDebugMessages ?? false;
            _maxLogs = dto.MaxLogs ?? 5;

            // Appearance
            _language = dto.Language ?? LanguageOption.en_GB;
            _easyLanguageSwitching = dto.EasyLanguageSwitching ?? programData.EasyLanguageSwitching;
            _theme = dto.Theme ?? ThemeOption.MatchWindows;
            _backdrop = dto.Backdrop ?? BackdropOption.AcrylicBase;
            _accentSource = dto.AccentSource ?? AccentSourceOption.MatchWindows;
            _customAccentColour = dto.CustomAccentColour ?? GetWindowsAccentColour();
            _windowTintSource = dto.WindowTintSource ?? WindowTintSourceOption.None;
            _customWindowTintColour = dto.CustomWindowTintColour ?? GetWindowsAccentColour();
            _windowTintOpacity = dto.WindowTintOpacity ?? 0.5;

            // Layout
            _rememberLayout = dto.RememberLayout ?? true;
            _openMaximised = dto.OpenMaximised ?? false;
            _defaultWidth = dto.DefaultWidth ?? 1600;
            _defaultHeight = dto.DefaultHeight ?? 900;

            // Backups
            _backupsFolder = dto.BackupsFolder ?? "";
            _maxBackups = dto.MaxBackups ?? 5;
            _automaticBackups = dto.AutomatedBackups ?? true;

            // API
            _apiTimeout = dto.ApiTimeout ?? 30;
            _apiMaxRetries = dto.ApiMaxRetries ?? 3;

            // Remote Database
            _databaseHost = dto.DatabaseHost ?? "localhost";
            _databasePort = dto.DatabasePort ?? 5432;
            _databaseName = dto.DatabaseName ?? "";
            _databaseUsername = dto.DatabaseUsername ?? "";
            _databasePassword = dto.DatabasePassword ?? "";
            _databaseConnectionTimeout = dto.DatabaseConnectionTimeout ?? 30;
            _databaseSslMode = dto.DatabaseSslMode ?? SslMode.Disable;

            // Search
            _searchCaseSensitive = dto.SearchCaseSensitive ?? false;
            _searchSplitQuery = dto.SearchSplitQuery ?? true;

            // Image Cache
            _imageCacheEnabled = dto.ImageCacheEnabled ?? true;
            _imageCacheWarnSizeGb = dto.ImageCacheWarnSizeGb ?? 1;
        }

        private static Color GetWindowsAccentColour() {
            return new UISettings().GetColorValue(UIColorType.AccentLight2);
        }
    }
}
