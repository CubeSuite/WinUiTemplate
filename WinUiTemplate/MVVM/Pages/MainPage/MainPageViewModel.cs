using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.MVVM.Models.ViewModels;

namespace WinUiTemplate.MVVM.Pages
{
    public partial class MainPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IBackupService backupService;

        // Constructors

        public MainPageViewModel(IServiceProvider serviceProvider) {
            serviceProvider.GetRequiredService<INotificationService>().NotificationRequested += OnNotificationRequested;

            INavigationService navigationService = serviceProvider.GetRequiredService<INavigationService>();
            navigationService.AllowNavigationChanged += OnNavigationAllowedChanged;
            NavMenuEnabled = navigationService.AllowNavigation;

            backupService = serviceProvider.GetRequiredService<IBackupService>();
            backupService.BackupCreated += OnBackupCreatedOrDeleted;
            backupService.BackupDeleted += OnBackupCreatedOrDeleted;

            Notifications = new ObservableCollection<NotificationViewModel>();

            CheckForBackups();
        }

        // Properties

        public ObservableCollection<NotificationViewModel> Notifications { get; }

        [ObservableProperty] public partial Visibility BackupsButtonVisibility { get; set; }
        [ObservableProperty] public partial bool NavMenuEnabled { get; set; }

        // Listeners

        private void OnNotificationRequested(NotificationViewModel notification) {
            Notifications.Add(notification);
        }

        private void OnBackupCreatedOrDeleted() {
            CheckForBackups();
        }

        private void OnNavigationAllowedChanged(bool allowed) {
            NavMenuEnabled = allowed;
        }

        // Private Functions

        private async void CheckForBackups() {
            bool backupsExist = (await backupService.GetBackupsAsync()).Count != 0;
            BackupsButtonVisibility = backupsExist ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
