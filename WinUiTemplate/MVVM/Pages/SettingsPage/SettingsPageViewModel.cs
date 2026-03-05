using CommunityToolkit.Mvvm.ComponentModel;
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
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUiTemplate.MVVM.Models.ViewModels.Settings;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.MVVM.Pages
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IUserSettings userSettings;
        private readonly IProgramData programData;
        private readonly IBackupService backupManager;
        private readonly INotificationService notificationService;
        private readonly IDialogService dialogService;
        private readonly IFileUtils fileUtils;
        private readonly IEncryptionService encryptionService;
        private readonly IArchiveService archiveService;

        // Properties
        public List<SettingsCategoryList> SettingsCategories { get; }

        // Constructors

        public SettingsPageViewModel(IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            programData = serviceProvider.GetRequiredService<IProgramData>();
            backupManager = serviceProvider.GetRequiredService<IBackupService>();
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
            archiveService = serviceProvider.GetRequiredService<IArchiveService>();

            // Logging

            SettingsCategories = new List<SettingsCategoryList>() {
                new SettingsCategoryList("Logging", [
                    new GenericSetting<bool>(
                        name: "Log Debug Messages",
                        description: "Whether debug messages should be logged to file. Enable to gather info for a bug report",
                        icon: "\uEBE8",
                        getValueFunc: () => userSettings.LogDebugMessages,
                        setValueFunc: (value) => userSettings.LogDebugMessages = value
                    ),
                    new ComparableSetting<int>(
                        name: "Max Logs",
                        description: "The maximum number of logs to keep",
                        icon: "\uEA37",
                        getValueFunc: () => userSettings.MaxLogs,
                        setValueFunc: (value) => userSettings.MaxLogs = value,
                        min: 1,
                        max: 10,
                        serviceProvider
                    ),
                    new ButtonSetting(
                        name: "Show Logs In Explorer",
                        description: $"Opens the folder that contains {programData.ProgramName}'s log files",
                        icon: "\uE8B7",
                        buttonText: "Show In Explorer",
                        onClick: () => {
                            OpenExplorer(programData.FilePaths.LogsFolder);
                            return Task.CompletedTask;
                        }
                    ),
                    new ButtonSetting(
                        name: "Show Crash Logs In Explorer",
                        description: $"Opens the folder that contains {programData.ProgramName}'s crashlog files",
                        icon: "\uE7BA",
                        buttonText: "Show In Explorer",
                        onClick: () => {
                            OpenExplorer(programData.FilePaths.CrashReportsFolder);
                            return Task.CompletedTask;
                        }
                    )
                ]),

                new SettingsCategoryList("Appearance", [
                    new GenericSetting<bool>(
                        name: "Open Maximised",
                        description: $"Whether {programData.ProgramName} should open in a maximised state",
                        icon: "\uE922",
                        getValueFunc: () => userSettings.OpenMaximised,
                        setValueFunc: (value) => userSettings.OpenMaximised = value
                    ),
                    new EnumSetting<IThemeService.Backdrop>(
                        name: "Backdrop",
                        description: $"Acrylic uses softer colours and is more transparent than Mica",
                        icon: "\uEB9F",
                        getValueFunc: () => userSettings.Backdrop,
                        setValueFunc: (value) => userSettings.Backdrop = value
                    ),
                    new GenericSetting<Color>(
                        name: "Accent Colour",
                        description: $"The accent colour to use for {programData.ProgramName}",
                        icon: "\uEF3C",
                        getValueFunc: () => userSettings.AccentColour.ToColor(),
                        setValueFunc: (value) => userSettings.AccentColour = value.ToHex()
                    ),
                    new ButtonSetting(
                        name: "Reset Accent Colour To System Default",
                        description: "Resets your accent colour to match the one you have chosen for Windows",
                        icon: "\uE790",
                        buttonText: "Reset",
                        onClick: () => {
                            userSettings.AccentColour = new UISettings().GetColorValue(UIColorType.AccentLight2).ToHex();
                            OnPropertyChanged(nameof(SettingsCategories));
                            return Task.CompletedTask;
                        }
                    )
                ]),
            };

            if (programData.EnableBackups) {
                SettingsCategories.Add(new SettingsCategoryList("Backups", [
                    new GenericSetting<bool>(
                        name: "Automatic Backups",
                        description: $"Whether to perform automatic backups when you close {programData.ProgramName}",
                        icon: "\uE74E",
                        getValueFunc: () => userSettings.AutomaticBackups,
                        setValueFunc: (value) => userSettings.AutomaticBackups = value
                    ),
                    new FilePathSetting(
                        name: "Backups Folder",
                        description: $"Where {programData.ProgramName} should store backups of its data",
                        icon: "\uE8B7",
                        getValueFunc: () => userSettings.BackupsFolder,
                        setValueFunc: (value) => userSettings.BackupsFolder = value,
                        serviceProvider,
                        type: FilePathSetting.PickerType.Folder
                    ),
                    new ComparableSetting<int>(
                        name: "Max Backups",
                        description: "The maximum number of backups to keep before deleting old ones",
                        icon: "\uEA37",
                        getValueFunc: () => userSettings.MaxBackups,
                        setValueFunc: (value) => userSettings.MaxBackups = value,
                        min: 1,
                        max: 10,
                        serviceProvider
                    ),
                    new ButtonSetting(
                        name: "Perform Backup",
                        description: "Backup your data now",
                        icon: "\uE78C",
                        buttonText: "Perform Backup",
                        onClick: PerformBackup
                    )
                ]));
            }

            if (programData.EncryptionLevel > Stores.EncryptionLevel.None) {
                SettingsCategories.Add(new SettingsCategoryList("Encryption", [
                    new ButtonSetting(
                        name: "Decrypt Data",
                        description: "Creates a .zip with your decrypted data to send to the developer for debugging.",
                        icon: "\uE785",
                        buttonText: "Decrypt",
                        onClick: DecryptData
                    )
                ]));
            }

            if (programData.UsesApi) {
                SettingsCategories.Add(new SettingsCategoryList("Internet", [
                    new ComparableSetting<int>(
                        name: "Request Timeout",
                        description: "If a request from the internet takes longer than this many seconds, it will be cancelled.",
                        icon: "\uE916",
                        getValueFunc: () => userSettings.ApiTimeout,
                        setValueFunc: (value) => userSettings.ApiTimeout = value,
                        min: 10,
                        max: 60,
                        serviceProvider
                    ),
                    new ComparableSetting<int>(
                        name: "Max Retries",
                        description: "A failed request from the internet will be retried this many times.",
                        icon: "\uE81C",
                        getValueFunc: () => userSettings.ApiTimeout,
                        setValueFunc: (value) => userSettings.ApiTimeout = value,
                        min: 0,
                        max: 5,
                        serviceProvider
                    )
                ]));
            }
        }

        // Button Handlers

        private void OpenExplorer(string folder) {
            Process.Start(new ProcessStartInfo() {
                FileName = folder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private async Task PerformBackup() {
            OperationResult result = await backupManager.CreateBackupAsync();
            if (!result.Success && result.Notify) {
                notificationService.Notify(InfoBarSeverity.Warning, "Backup Failed", result.ErrorMessage ?? "");
            }

            await Task.Delay(1000); // Show spinner
        }

        private async Task DecryptData() {
            if (!await dialogService.Confirm(
                "Are You Sure?",
                "The resulting archive may contain sensative information.\n\n" +
                "You need to carefully check it and remove any data you do not wish to share.\n\n" +
                "Encrypted Settings such as API keys will not be decrypted.\n\n" +
                "Do not share this file with anyone that you do not trust."
            )) {
                return;
            }

            StorageFile? zipLocation = await dialogService.PickSaveLocation(fileUtils.GetFileSafeTimestamp(), ".zip");
            if (zipLocation == null) return;

            FolderResult tempResult = await fileUtils.TryGetOrCreateFolderAsync(Path.Combine(programData.FilePaths.RootFolder, "Temp"));
            if (!tempResult.Success || tempResult.Folder == null) {
                notificationService.Notify(
                    InfoBarSeverity.Error, "Failed To Decrypt",
                    $"{programData.ProgramName} wasn't able to create a temporary folder"
                );

                return;
            }

            string rootPath = programData.FilePaths.RootFolder;
            FilesResult filesResult = await fileUtils.TryGetAllFilesAsync(rootPath);
            if (!filesResult.Success || filesResult.Files == null) {
                notificationService.Notify(
                    InfoBarSeverity.Error, "Failed To Decrypt",
                    $"{programData.ProgramName} wasn't able to copy data files to a temporary folder"
                );

                return;
            }

            foreach (StorageFile file in filesResult.Files) {
                if (file.Name == "EncryptionKey.bin") continue;

                FileReadResult readResult = await fileUtils.TryReadFileAsync(file.Path);
                if(!readResult.Success || string.IsNullOrEmpty(readResult.Content)) {
                    notificationService.Notify(
                        InfoBarSeverity.Error, "Failed To Decrypt",
                        $"{programData.ProgramName} wasn't able to read the file '{Path.GetFileName(file.Path)}'"
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
                    InfoBarSeverity.Error, "Failed To Decrypt",
                    $"{programData.ProgramName} wasn't able to zip your decrypted data"
                );

                if(await dialogService.Confirm(new DialogOptions(
                    MessageType.None, "Delete Decrypted Data?", 
                    "Would you like to delete your decrypted data, or view it in Explorer?",
                    PrimaryText: "Delete",
                    SecondaryText: "View In Explorer",
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
                    InfoBarSeverity.Success, "Decrypted Archive Created", 
                    "Your decrypted data archive has been created successfully"
                );
            }
        }
    }
}
