using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels
{
    public partial class NotificationViewModel : ObservableObject
    {
        // Fields
        private bool closeOnButtonClick = true;

        // Properties
        public InfoBarSeverity Severity { get; }
        public string Title { get; }
        public string Message { get; }
        public string ButtonText { get; }

        [ObservableProperty] public partial bool IsOpen { get; set; }

        public Action? OnButtonClicked { get; }
        public Visibility ActionButtonVisibility => ButtonText == "" ? Visibility.Collapsed : Visibility.Visible;

        // Constructors

        public NotificationViewModel(InfoBarSeverity severity, string title, string message, string buttonText = "", Action? onClick = null, bool closeOnButtonClick = true) {
            Severity = severity;
            Title = title;
            Message = message;
            IsOpen = true;
            ButtonText = buttonText;
            OnButtonClicked = onClick;
            this.closeOnButtonClick = closeOnButtonClick;

            OnPropertyChanged(nameof(ActionButtonVisibility));
        }

        // Commands

        [RelayCommand]
        private async Task ActionButtonClicked() {
            OnButtonClicked?.Invoke();
            IsOpen = !closeOnButtonClick;
        }
    }
}
