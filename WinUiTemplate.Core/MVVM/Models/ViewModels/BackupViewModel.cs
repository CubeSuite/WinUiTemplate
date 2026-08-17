using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels
{
    public partial class BackupViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IBackupService backupService;
        private readonly IUserSettings userSettings;
        private readonly ILanguageService lang;

        // Fields
        private string zipFile;
        private bool isRestoring = false;
        private CancellationTokenSource tokenSource;

        // Properties

        public string RestoreButtonText => isRestoring ? lang.Get("CancelButtonText") : lang.Get("RestoreButtonText");
        public string Timestamp { get; set; }
        public string CreatedWith { get; set; }
        public string Size { get; set; }

        // Constructors

        public BackupViewModel(BackupInfo info, IServiceProvider serviceProvider) {
            backupService = serviceProvider.GetRequiredService<IBackupService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            lang = new LanguageService("BackupViewModel");

            tokenSource = new CancellationTokenSource();
            zipFile = info.Path;

            Timestamp = info.Created.ToString("g", lang.GetCulture(userSettings.Language));
            CreatedWith = info.CreatedWith.ToString();
            Size = SizeToString(info.Size);
        }

        // Events

        public event Action<BackupViewModel>? BackupDeleted;

        // Commands

        [RelayCommand]
        private async Task Restore() {
            if (!isRestoring) {
                isRestoring = true;
                OnPropertyChanged(nameof(RestoreButtonText));
                await backupService.RestoreBackupAsync(zipFile, tokenSource.Token);
            }
            else {
                tokenSource.Cancel();
                isRestoring = false;
                OnPropertyChanged(nameof(RestoreButtonText));
            }
        }

        [RelayCommand]
        private async Task Delete() {
            await backupService.DeleteBackupAsync(zipFile, tokenSource.Token);
            BackupDeleted?.Invoke(this);
        }

        // Private Functions

        private string SizeToString(long size) {
            string[] sizes = { "B", "kB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

    }
}
