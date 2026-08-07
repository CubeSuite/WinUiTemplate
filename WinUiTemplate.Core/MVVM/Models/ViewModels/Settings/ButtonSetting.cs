using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels.Settings
{
    public partial class ButtonSetting : SettingBase
    {
        // Services & Stores
        private readonly INotificationService notificationService;

        // Properties

        public string ButtonText { get; }
        public Func<Task> OnClick { get; }

        [ObservableProperty]
        public partial Visibility LoaderVisibility { get; set; }

        // Commands

        [RelayCommand]
        private async Task ExecuteButtonAction() {
            if (OnClick == null) return;
            
            LoaderVisibility = Visibility.Visible;
            try {
                await OnClick();
            }
            catch (Exception e) {
                notificationService.Notify(
                    InfoBarSeverity.Error, $"Failed To Execute Action For '{Name}'", 
                    $"An error occurred while executing the action: {e.Message}"
                );
            }
            finally {
                LoaderVisibility = Visibility.Collapsed;
            }
        }

        // Constructors

        public ButtonSetting(string name, string description, string icon,
                             string buttonText, Func<Task> onClick, IServiceProvider serviceProvider, Func<bool>? isVisibleFunc = null)
                            :base(name, description, icon, "Button")
        {
            notificationService = serviceProvider.GetRequiredService<INotificationService>();

            ButtonText = buttonText;
            OnClick = onClick;
            getIsVisibleFunc = isVisibleFunc;

            LoaderVisibility = Visibility.Collapsed;
        }
    }
}
