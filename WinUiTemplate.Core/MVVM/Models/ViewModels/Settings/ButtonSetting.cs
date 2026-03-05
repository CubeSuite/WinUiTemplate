using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public partial class ButtonSetting : SettingBase
    {
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
            await OnClick();
            LoaderVisibility = Visibility.Collapsed;
        }

        // Constructors

        public ButtonSetting(string name, string description, string icon,
                             string buttonText, Func<Task> onClick)
                            :base(name, description, icon, "Button")
        {
            ButtonText = buttonText;
            OnClick = onClick;

            LoaderVisibility = Visibility.Collapsed;
        }
    }
}
