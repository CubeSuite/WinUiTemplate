using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.MVVM.Models.ViewModels;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.MVVM.Pages
{
    public class BackupsPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IBackupService backupService;

        // Members
        private CancellationTokenSource tokenSource;

        // Properties
        public ObservableCollection<BackupViewModel> Backups { get; }

        // Constructors

        public BackupsPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            backupService = serviceProvider.GetRequiredService<IBackupService>();

            tokenSource = new CancellationTokenSource();

            Backups = new ObservableCollection<BackupViewModel>();
            LoadBackups();
        }

        // Listeners

        private void OnBackupDeleted(BackupViewModel backup) {
            Backups.Remove(backup);
        }

        // Private Functions

        private async void LoadBackups() {
            IReadOnlyList<BackupInfo> backups = await backupService.GetBackupsAsync(tokenSource.Token);
            if (backups == null) return;

            foreach (BackupInfo zipFile in backups) {
                BackupViewModel backup = new BackupViewModel(zipFile, serviceProvider);
                backup.BackupDeleted += OnBackupDeleted;
                Backups.Add(backup);
            }
        }
    }
}
