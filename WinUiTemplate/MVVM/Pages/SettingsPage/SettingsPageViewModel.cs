using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Windows.Devices.Gpio.Provider;
using Windows.Security.Authentication.Web.Provider;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUiTemplate.Core.MVVM.Models.ViewModels.Settings;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;
using Npgsql;
using WinUiTemplate.Core.Services;

namespace WinUiTemplate.MVVM.Pages
{
    public partial class SettingsPageViewModel : ObservableObject, IDisposable
    {
        // Services & Stores
        private readonly INotificationService notificationService;
        private readonly IEncryptionService encryptionService;
        private readonly IArchiveService archiveService;
        private readonly IBackupService backupManager;
        private readonly IDialogService dialogService;
        private readonly IUserSettings userSettings;
        private readonly IProgramData programData;
        private readonly IImageCache imageCache;
        private readonly IFileUtils fileUtils;

        private readonly ILanguageService lang;

        // Properties
        public List<SettingsCategoryList> SettingsCategories { get; }

        // Constructors

        public SettingsPageViewModel(IServiceProvider serviceProvider) {
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
            archiveService = serviceProvider.GetRequiredService<IArchiveService>();
            backupManager = serviceProvider.GetRequiredService<IBackupService>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            programData = serviceProvider.GetRequiredService<IProgramData>();
            imageCache = serviceProvider.GetRequiredService<IImageCache>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();

            lang = new LanguageService("SettingsPageViewModel");

            userSettings.SettingChanged += OnSettingChanged;

            SettingsCategories = new List<SettingsCategoryList>() {
                new SettingsCategoryList(lang.Get("LoggingCategory"), [
                    new GenericSetting<bool>(
                        name: lang.Get("LogDebugMessagesName"),
                        description: lang.Get("LogDebugMessagesDescription"),
                        icon: "\uEBE8",
                        getValueFunc: () => userSettings.LogDebugMessages,
                        setValueFunc: (value) => userSettings.LogDebugMessages = value
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("MaxLogsName"),
                        description: lang.Get("MaxLogsDescription"),
                        icon: "\uEA37",
                        getValueFunc: () => userSettings.MaxLogs,
                        setValueFunc: (value) => userSettings.MaxLogs = value,
                        min: 1,
                        max: 10,
                        serviceProvider
                    ),
                    new ButtonSetting(
                        name: lang.Get("ShowLogsInExplorerName"),
                        description: string.Format(lang.Get("ShowLogsInExplorerDescription"), programData.ProgramName),
                        icon: "\uE8B7",
                        buttonText: lang.Get("ShowInExplorerButtonText"),
                        onClick: () => {
                            OpenExplorer(programData.FilePaths.LogsFolder);
                            return Task.CompletedTask;
                        },
                        serviceProvider
                    ),
                    new ButtonSetting(
                        name: lang.Get("ShowCrashLogsInExplorerName"),
                        description: string.Format(lang.Get("ShowCrashLogsInExplorerDescription"), programData.ProgramName),
                        icon: "\uE7BA",
                        buttonText: lang.Get("ShowInExplorerButtonText"),
                        onClick: () => {
                            OpenExplorer(programData.FilePaths.CrashReportsFolder);
                            return Task.CompletedTask;
                        },
                        serviceProvider
                    )
                ]),

                new SettingsCategoryList(lang.Get("AppearanceCategory"), [
                    new EnumSetting<LanguageOption>(
                        name: lang.Get("LanguageName"),
                        description: lang.Get("LanguageDescription"),
                        icon: "\uF2B7",
                        getValueFunc: () => userSettings.Language,
                        setValueFunc: LanguageChanged
                    ),
                    new GenericSetting<bool>(
                        name: lang.Get("EasyLanguageSwitchingName"),
                        description: string.Format(lang.Get("EasyLanguageSwitchingDescription"), programData.ProgramName),
                        icon: "\uF2B7",
                        getValueFunc: () => userSettings.EasyLanguageSwitching,
                        setValueFunc: (value) => userSettings.EasyLanguageSwitching = value
                    ),
                    new EnumSetting<ThemeOption>(
                        name: lang.Get("ThemeName"),
                        description: lang.Get("ThemeDescription"),
                        icon: "\uF0CE",
                        getValueFunc: () => userSettings.Theme,
                        setValueFunc: (value) => userSettings.Theme = value
                    ),
                    new EnumSetting<BackdropOption>(
                        name: lang.Get("BackdropName"),
                        description: lang.Get("BackdropDescription"),
                        icon: "\uEB9F",
                        getValueFunc: () => userSettings.Backdrop,
                        setValueFunc: (value) => userSettings.Backdrop = value
                    ),
                    new EnumSetting<AccentSourceOption>(
                        name: lang.Get("AccentColourSourceName"),
                        description: lang.Get("AccentColourSourceDescription"),
                        icon: "\uE790",
                        getValueFunc: () => userSettings.AccentSource,
                        setValueFunc: (value) => userSettings.AccentSource = value
                    ),
                    new GenericSetting<Color>(
                        name: lang.Get("CustomAccentColourName"),
                        description: string.Format(lang.Get("CustomAccentColourDescription"), programData.ProgramName),
                        icon: "\uE73C",
                        getValueFunc: () => userSettings.CustomAccentColour,
                        setValueFunc: (value) => userSettings.CustomAccentColour = value,
                        type: "",
                        isVisibleFunc: () => userSettings.AccentSource == AccentSourceOption.Custom
                    ),
                    new EnumSetting<WindowTintSourceOption>(
                        name: lang.Get("WindowTintSourceName"),
                        description: lang.Get("WindowTintSourceDescription"),
                        icon: "\uEF3C",
                        getValueFunc: () => userSettings.WindowTintSource,
                        setValueFunc: (value) => userSettings.WindowTintSource = value
                    ),
                    new GenericSetting<Color>(
                        name: lang.Get("CustomWindowTintColourName"),
                        description: string.Format(lang.Get("CustomWindowTintColourDescription"), programData.ProgramName),
                        icon: "\uE73C",
                        getValueFunc: () => userSettings.CustomWindowTintColour,
                        setValueFunc: (value) => userSettings.CustomWindowTintColour = value,
                        type: "",
                        isVisibleFunc: () => userSettings.WindowTintSource == WindowTintSourceOption.Custom
                    ),
                    new ComparableSetting<double>(
                        name: lang.Get("WindowTintOpacityName"),
                        description: lang.Get("WindowTintOpacityDescription"),
                        icon: "\uE793",
                        getValueFunc: () => userSettings.WindowTintOpacity * 100.0,
                        setValueFunc: (value) => userSettings.WindowTintOpacity = value / 100.0,
                        min: 0.0,
                        max: 100.0,
                        serviceProvider,
                        isVisibleFunc: () => userSettings.WindowTintSource != WindowTintSourceOption.None
                    )
                ]),

                new SettingsCategoryList(lang.Get("LayoutCategory"), [
                    new GenericSetting<bool>(
                        name: lang.Get("RememberLayoutName"),
                        description: string.Format(lang.Get("RememberLayoutDescription"), programData.ProgramName),
                        icon: "\uE9A6",
                        getValueFunc: () => userSettings.RememberLayout,
                        setValueFunc: (value) => userSettings.RememberLayout = value
                    ),
                    new GenericSetting<bool>(
                        name: lang.Get("OpenMaximisedName"),
                        description: string.Format(lang.Get("OpenMaximisedDescription"), programData.ProgramName),
                        icon: "\uE922",
                        getValueFunc: () => userSettings.OpenMaximised,
                        setValueFunc: (value) => userSettings.OpenMaximised = value
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("DefaultWidthName"),
                        description: string.Format(lang.Get("DefaultWidthDescription"), programData.ProgramName),
                        icon: "\uE72A",
                        getValueFunc: () => userSettings.DefaultWidth,
                        setValueFunc: (value) => userSettings.DefaultWidth = value,
                        min: 100,
                        max: 10000,
                        serviceProvider
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("DefaultHeightName"),
                        description: string.Format(lang.Get("DefaultHeightDescription"), programData.ProgramName),
                        icon: "\uE74B",
                        getValueFunc: () => userSettings.DefaultHeight,
                        setValueFunc: (value) => userSettings.DefaultHeight = value,
                        min: 100,
                        max: 10000,
                        serviceProvider
                    ),
                ]),

                new SettingsCategoryList(lang.Get("SearchCategory"), [
                    new GenericSetting<bool>(
                    name: lang.Get("CaseSensitiveName"),
                    description: lang.Get("CaseSensitiveDescription"),
                    icon: "\uE84A",
                    getValueFunc: () => userSettings.SearchCaseSensitive,
                    setValueFunc: (value) => userSettings.SearchCaseSensitive = value
                ),
                new GenericSetting<bool>(
                    name: lang.Get("SplitSearchQueryName"),
                    description: lang.Get("SplitSearchQueryDescription"),
                    icon: "\uE8C6",
                    getValueFunc: () => userSettings.SearchSplitQuery,
                    setValueFunc: (value) => userSettings.SearchSplitQuery = value
                )
                ]),

                new SettingsCategoryList(lang.Get("EncryptionCategory"), [
                    new ButtonSetting(
                        name: lang.Get("DecryptDataName"),
                        description: lang.Get("DecryptDataDescription"),
                        icon: "\uE785",
                        buttonText: lang.Get("DecryptButtonText"),
                        onClick: DecryptData,
                        serviceProvider
                    )
                ]),

                new SettingsCategoryList(lang.Get("ImageCacheCategory"), [
                    new GenericSetting<bool>(
                        name: lang.Get("CacheImagesName"),
                        description: lang.Get("CacheImagesDescription"),
                        icon: "\uE78C",
                        getValueFunc: () => userSettings.ImageCacheEnabled,
                        setValueFunc: (value) => userSettings.ImageCacheEnabled = value
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("ImageCacheSizeWarningLimitName"),
                        description: lang.Get("ImageCacheSizeWarningLimitDescription"),
                        icon: "\uE7BA",
                        getValueFunc: () => userSettings.ImageCacheWarnSizeGb,
                        setValueFunc: (value) => userSettings.ImageCacheWarnSizeGb = value,
                        min: 1,
                        max: 1024,
                        serviceProvider,
                        isVisibleFunc: () => userSettings.ImageCacheEnabled
                    ),
                    new ButtonSetting(
                        name: string.Format(lang.Get("ClearImageCacheName"), imageCache.CacheSize),
                        description: lang.Get("ClearImageCacheDescription"),
                        icon: "\uE74D",
                        buttonText: lang.Get("ClearButtonText"),
                        onClick: ClearImageCache,
                        serviceProvider,
                        isVisibleFunc: () => userSettings.ImageCacheEnabled
                    )
                ])
            };

            if (programData.EnableBackups) {
                SettingsCategories.Add(new SettingsCategoryList(lang.Get("BackupsCategory"), [
                    new GenericSetting<bool>(
                        name: lang.Get("AutomaticBackupsName"),
                        description: string.Format(lang.Get("AutomaticBackupsDescription"), programData.ProgramName),
                        icon: "\uE74E",
                        getValueFunc: () => userSettings.AutomaticBackups,
                        setValueFunc: (value) => userSettings.AutomaticBackups = value
                    ),
                    new FilePathSetting(
                        name: lang.Get("BackupsFolderName"),
                        description: string.Format(lang.Get("BackupsFolderDescription"), programData.ProgramName),
                        icon: "\uE8B7",
                        getValueFunc: () => userSettings.BackupsFolder,
                        setValueFunc: async (value) => await PickBackupsFolder(value),
                        serviceProvider,
                        type: FilePathSetting.PickerType.Folder
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("MaxBackupsName"),
                        description: lang.Get("MaxBackupsDescription"),
                        icon: "\uEA37",
                        getValueFunc: () => userSettings.MaxBackups,
                        setValueFunc: (value) => userSettings.MaxBackups = value,
                        min: 1,
                        max: 10,
                        serviceProvider
                    ),
                    new ButtonSetting(
                        name: lang.Get("PerformBackupName"),
                        description: lang.Get("PerformBackupDescription"),
                        icon: "\uE78C",
                        buttonText: lang.Get("PerformBackupButtonText"),
                        onClick: PerformBackup,
                        serviceProvider
                    )
                ]));
            }

            if (programData.UsesApi) {
                SettingsCategories.Add(new SettingsCategoryList(lang.Get("InternetCategory"), [
                    new ComparableSetting<int>(
                        name: lang.Get("RequestTimeoutName"),
                        description: lang.Get("RequestTimeoutDescription"),
                        icon: "\uE916",
                        getValueFunc: () => userSettings.ApiTimeout,
                        setValueFunc: (value) => userSettings.ApiTimeout = value,
                        min: 10,
                        max: 60,
                        serviceProvider
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("MaxRetriesName"),
                        description: lang.Get("MaxRetriesDescription"),
                        icon: "\uE81C",
                        getValueFunc: () => userSettings.ApiMaxRetries,
                        setValueFunc: (value) => userSettings.ApiMaxRetries = value,
                        min: 0,
                        max: 5,
                        serviceProvider
                    )
                ]));
            }

            if (programData.UsesRemoteDatabase) {
                SettingsCategories.Add(new SettingsCategoryList(lang.Get("DatabaseCategory"), [
                    new EncryptedSetting(
                        name: lang.Get("DatabaseHostName"),
                        description: lang.Get("DatabaseHostDescription"),
                        icon: "\uE968",
                        getValueFunc: () => userSettings.DatabaseHost,
                        setValueFunc: (value) => userSettings.DatabaseHost = value,
                        serviceProvider
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("DatabasePortName"),
                        description: lang.Get("DatabasePortDescription"),
                        icon: "\uE8AB",
                        getValueFunc: () => userSettings.DatabasePort,
                        setValueFunc: (value) => userSettings.DatabasePort = value,
                        min: 1,
                        max: 65535,
                        serviceProvider
                    ),
                    new GenericSetting<string>(
                        name: lang.Get("DatabaseNameName"),
                        description: lang.Get("DatabaseNameDescription"),
                        icon: "\uE74E",
                        getValueFunc: () => userSettings.DatabaseName,
                        setValueFunc: (value) => userSettings.DatabaseName = value
                    ),
                    new EncryptedSetting(
                        name: lang.Get("UsernameName"),
                        description: lang.Get("UsernameDescription"),
                        icon: "\uE77B",
                        getValueFunc: () => userSettings.DatabaseUsername,
                        setValueFunc: (value) => userSettings.DatabaseUsername = value,
                        serviceProvider
                    ),
                    new EncryptedSetting(
                        name: lang.Get("PasswordName"),
                        description: lang.Get("PasswordDescription"),
                        icon: "\uE72E",
                        getValueFunc: () => userSettings.DatabasePassword,
                        setValueFunc: (value) => userSettings.DatabasePassword = value,
                        serviceProvider
                    ),
                    new ComparableSetting<int>(
                        name: lang.Get("ConnectionTimeoutName"),
                        description: lang.Get("ConnectionTimeoutDescription"),
                        icon: "\uE916",
                        getValueFunc: () => userSettings.DatabaseConnectionTimeout,
                        setValueFunc: (value) => userSettings.DatabaseConnectionTimeout = value,
                        min: 5,
                        max: 120,
                        serviceProvider
                    ),
                    new EnumSetting<SslMode>(
                        name: lang.Get("SslModeName"),
                        description: lang.Get("SslModeDescription"),
                        icon: "\uE72E",
                        getValueFunc: () => userSettings.DatabaseSslMode,
                        setValueFunc: (value) => userSettings.DatabaseSslMode = value
                    )
                ]));
            }
        }

        // Listeners

        private void OnSettingChanged(string settingName) {
            foreach(SettingsCategoryList category in SettingsCategories) {
                foreach(SettingBase setting in category.Settings) {
                    setting.NotifyIsVisibilityChanged();
                    setting.NotifyValueChanged();
                }
            }
        }

        // Commands

        [RelayCommand]
        private async Task RestoreDefaults() {
            if (await dialogService.Confirm(
                lang.Get("RestoreDefaultsDialogTitle"), 
                lang.Get("RestoreDefaultsDialogMessage"))
            ) {
                userSettings.RestoreDefaults();

                foreach (SettingsCategoryList category in SettingsCategories) {
                    foreach (SettingBase setting in category.Settings) {
                        setting.NotifyValueChanged();
                    }
                }
            }
        }

        // Button Handlers

        private async void LanguageChanged(LanguageOption value) {
            if (userSettings.Language == value) return;

            await userSettings.ChangeLanguageAsync(value);
        }

        private void OpenExplorer(string folder) {
            Process.Start(new ProcessStartInfo() {
                FileName = folder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private async Task PickBackupsFolder(string folder) {
            if (folder == null) return;

            string appFolder = Path.GetDirectoryName(programData.FilePaths.RootFolder) ?? "";
            if (folder.StartsWith(appFolder)) {
                notificationService.Notify(InfoBarSeverity.Error, lang.Get("InvalidBackupsFolderTitle"), 
                    string.Format(lang.Get("InvalidBackupsFolderMessage"), programData.ProgramName)
                );
            }
            else {
                userSettings.BackupsFolder = folder;
            }
        }

        private async Task PerformBackup() {
            OperationResult result = await backupManager.CreateBackupAsync();
            if (!result.Success && result.Notify) {
                notificationService.Notify(InfoBarSeverity.Warning, lang.Get("BackupFailedTitle"), result.ErrorMessage ?? "");
            }

            await Task.Delay(1000); // Show spinner
        }

        private async Task DecryptData() {
            if (!await dialogService.Confirm(
                lang.Get("DecryptConfirmTitle"),
                lang.Get("DecryptConfirmMessage")
            )) {
                return;
            }

            StorageFile? zipLocation = await dialogService.PickSaveLocation(fileUtils.GetFileSafeTimestamp(), ".zip");
            if (zipLocation == null) return;

            FolderResult tempResult = await fileUtils.TryGetOrCreateFolderAsync(Path.Combine(programData.FilePaths.RootFolder, "Temp"));
            if (!tempResult.Success || tempResult.Folder == null) {
                notificationService.Notify(
                    InfoBarSeverity.Error, lang.Get("FailedToDecryptTitle"),
                    string.Format(lang.Get("FailedToCreateTempFolderMessage"), programData.ProgramName)
                );

                return;
            }

            string rootPath = programData.FilePaths.RootFolder;
            FilesResult filesResult = await fileUtils.TryGetAllFilesAsync(rootPath);
            if (!filesResult.Success || filesResult.Files == null) {
                notificationService.Notify(
                    InfoBarSeverity.Error, lang.Get("FailedToDecryptTitle"),
                    string.Format(lang.Get("FailedToCopyFilesMessage"), programData.ProgramName)
                );

                return;
            }

            foreach (StorageFile file in filesResult.Files) {
                if (file.Name == "EncryptionKey.bin") continue;

                FileReadResult readResult = await fileUtils.TryReadFileAsync(file.Path);
                if(!readResult.Success || string.IsNullOrEmpty(readResult.Content)) {
                    notificationService.Notify(
                        InfoBarSeverity.Error, lang.Get("FailedToDecryptTitle"),
                        string.Format(lang.Get("FailedToReadFileMessage"), programData.ProgramName, Path.GetFileName(file.Path))
                    );

                    return;
                }

                string relativePath = fileUtils.GetRelativePath(programData.FilePaths.RootFolder, file.Path);
                string newPath = Path.Combine(tempResult.Folder.Path, relativePath);
                string? parent = Path.GetDirectoryName(newPath);
                if(parent != null) {
                    await fileUtils.TryGetOrCreateFolderAsync(parent);
                }

                bool encrypted = readResult.Content.StartsWith(fileUtils.EncryptedFileHeader);
                string content = encrypted ? await encryptionService.DecryptFromBase64Async(readResult.Content) : readResult.Content;
                await fileUtils.TryWriteFileAsync(newPath, content);
            }

            OperationResult zipResult = await archiveService.ZipFolderAsync(tempResult.Folder.Path, zipLocation.Path);
            if (!zipResult.Success) {
                notificationService.Notify(
                    InfoBarSeverity.Error, lang.Get("FailedToDecryptTitle"),
                    string.Format(lang.Get("FailedToZipDataMessage"), programData.ProgramName)
                );

                if(await dialogService.Confirm(new DialogOptions(
                    MessageType.None, lang.Get("DeleteDecryptedDataTitle"), 
                    lang.Get("DeleteDecryptedDataMessage"),
                    PrimaryText: lang.Get("DeleteButtonText"),
                    SecondaryText: lang.Get("ViewInExplorerButtonText"),
                    CloseText: ""
                ))) {
                    await tempResult.Folder.DeleteAsync();
                }
                else {
                    OpenExplorer(tempResult.Folder.Path);
                }
            }
            else {
                await tempResult.Folder.DeleteAsync();
                notificationService.Notify(
                    InfoBarSeverity.Success, lang.Get("DecryptedArchiveCreatedTitle"), 
                    lang.Get("DecryptedArchiveCreatedMessage")
                );
            }
        }

        private async Task ClearImageCache() {
            OperationResult result = await imageCache.ClearCache();
            if (!result.Notify) return;

            if (result) {
                notificationService.Notify(InfoBarSeverity.Success, lang.Get("ImageCacheClearedTitle"));
            }
            else {
                notificationService.Notify(InfoBarSeverity.Error, lang.Get("FailedToClearImageCacheTitle"), result.ErrorMessage ?? lang.Get("UnknownErrorMessage"));
            }
        }

        // IDisposable

        public void Dispose() {
            userSettings.SettingChanged -= OnSettingChanged;
        }
    }
}
