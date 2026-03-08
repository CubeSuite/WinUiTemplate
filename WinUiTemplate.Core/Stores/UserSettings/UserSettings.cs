using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using WinUiTemplate.MVVM.Models.ViewModels.Settings;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Stores
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
        private readonly IProgramData programData;
        private readonly IFileUtils fileUtils;
        private readonly ILoggerService logger;
        private readonly INotificationService notificationService;

        // Members

        private bool _loaded = false;
        private bool _isFirstLaunch = true;
        
        private bool _logDebugMessages = false;
        private int _maxLogs = 5;

        private bool _darkMode = true;
        private bool _rememberLayout = true;
        private bool _openMaximised = false;
        private int _defaultWidth = 1600;
        private int _defaultHeight = 900;
        private IThemeService.Backdrop _backdrop = IThemeService.Backdrop.Acrylic;
        private string _accentColour = "";

        private string _backupsFolder = "";
        private int _maxBackups = 5;
        private bool _automaticBackups = true;

        private int _apiTimeout = 10;
        private int _apiMaxRetries = 3;

        private readonly object saveLock = new object();
        private CancellationTokenSource? tokenSource;

        private const int saveDebounceDelayMs = 200;

        private record SettingsDTO(
            bool IsFirstLaunch,
            bool LogDebugMessages,
            int MaxLogs,
            bool DarkMode,
            bool RememberLayout,
            bool OpenMaximised,
            int DefaultWidth,
            int DefaultHeight,
            IThemeService.Backdrop Backdrop,
            string AccentColour,
            string BackupsFolder,
            int MaxBackups,
            bool AutomatedBackups,
            int ApiTimeout,
            int ApiMaxRetries
        );

        // Properties

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
            set {
                if (_isFirstLaunch == value) return;
                _isFirstLaunch = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(IsFirstLaunch));
            }
        }

        public bool LogDebugMessages {
            get => _logDebugMessages;
            set {
                if (_logDebugMessages == value) return;
                _logDebugMessages = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(LogDebugMessages));
            }
        }

        public int MaxLogs {
            get => _maxLogs;
            set {
                if (_maxLogs == value) return;
                _maxLogs = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(MaxLogs));
            }
        }

        public bool DarkMode {
            get => _darkMode;
            set {
                if (_darkMode == value) return;
                _darkMode = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(DarkMode));
            }
        }

        public bool RememberLayout {
            get => _rememberLayout;
            set {
                if (_rememberLayout == value) return;
                _rememberLayout = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(RememberLayout));
            }
        }

        public bool OpenMaximised {
            get => _openMaximised;
            set {
                if (_openMaximised == value) return;
                _openMaximised = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(OpenMaximised));
            }
        }

        public int DefaultWidth {
            get => _defaultWidth;
            set {
                if (_defaultWidth == value) return;
                _defaultWidth = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(DefaultWidth));
            }
        }

        public int DefaultHeight {
            get => _defaultHeight;
            set {
                if (_defaultHeight == value) return;
                _defaultHeight = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(DefaultHeight));
            }
        }

        public IThemeService.Backdrop Backdrop {
            get => _backdrop;
            set {
                if (_backdrop == value) return;
                _backdrop = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(Backdrop));
            }
        }

        public string AccentColour {
            get => _accentColour;
            set {
                if (_accentColour == value) return;
                _accentColour = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(AccentColour));
            }
        }

        public string BackupsFolder {
            get => _backupsFolder;
            set {
                if (_backupsFolder == value) return;
                _backupsFolder = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(BackupsFolder));
            }
        }

        public int MaxBackups {
            get => _maxBackups;
            set {
                if (_maxBackups == value) return;
                _maxBackups = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(MaxBackups));
            }
        }

        public bool AutomaticBackups {
            get => _automaticBackups;
            set {
                if (_automaticBackups == value) return;
                _automaticBackups = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(AutomaticBackups));
            }
        }

        public int ApiTimeout {
            get => _apiTimeout;
            set {
                if (_apiTimeout == value) return;
                _apiTimeout = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(ApiTimeout));
            }
        }

        public int ApiMaxRetries {
            get => _apiMaxRetries;
            set {
                if (_apiMaxRetries == value) return;
                _apiMaxRetries = value;
                if (Loaded) DebounceSave();
                SettingChanged?.Invoke(nameof(ApiMaxRetries));
            }
        }

        // Constructors

        public UserSettings(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
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

                SettingsDTO? dto = JsonConvert.DeserializeObject<SettingsDTO>(result.Content ?? "{}");
                if(dto == null) {
                    string error = $"Parsed Settings.json is null";
                    Debug.Assert(false, error);
                    logger.LogError(error);
                    Loaded = true;
                    return;
                }

                LoadFromDTO(dto);
                Loaded = true;
                logger.LogInfo("Loaded UserSettings");
            }
            catch (Exception e) {
                Debug.Assert(false, $"UserSettings.Load failed: '{e.Message}'");
                Loaded = true;
            }
        }

        public void RestoreDefaults() {
            LogDebugMessages = true;
            MaxLogs = 5;
            OpenMaximised = false;
            MaxBackups = 5;
            // Don't restore BackupsFolder and AutomaticBackups
        }

        // Private Functions

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
                        catch (TaskCanceledException) {} // Expected
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
        }

        private SettingsDTO ToDTO() => new SettingsDTO(
            _isFirstLaunch,
            _logDebugMessages,
            _maxLogs,
            _darkMode,
            _rememberLayout,
            _openMaximised,
            _defaultWidth,
            _defaultHeight,
            _backdrop,
            _accentColour,
            _backupsFolder,
            _maxBackups,
            _automaticBackups,
            _apiTimeout,
            _apiMaxRetries
        );

        private void LoadFromDTO(SettingsDTO dto) {
            _isFirstLaunch = dto.IsFirstLaunch;
            _logDebugMessages = dto.LogDebugMessages;
            _maxLogs = dto.MaxLogs;
            _darkMode = dto.DarkMode;
            _rememberLayout = dto.RememberLayout;
            _openMaximised = dto.OpenMaximised;
            _defaultWidth = dto.DefaultWidth;
            _defaultHeight = dto.DefaultHeight;
            _backdrop = dto.Backdrop;
            _accentColour = dto.AccentColour;
            _backupsFolder = dto.BackupsFolder;
            _maxBackups = dto.MaxBackups;
            _automaticBackups = dto.AutomatedBackups;
            _apiTimeout = dto.ApiTimeout;
            _apiMaxRetries = dto.ApiMaxRetries;
        }
    }
}
