using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.ViewModels
{
    public partial class NotificationViewModel : ObservableObject
    {
        // Properties
        public InfoBarSeverity Severity { get; }
        public string Title { get; }
        public string Message { get; }
        public string ButtonText { get; }
        public Action? OnButtonClicked { get; }

        public Visibility ActionButtonVisibility => ButtonText == "" ? Visibility.Collapsed : Visibility.Visible;

        // Constructors

        public NotificationViewModel(InfoBarSeverity severity, string title, string message, string buttonText = "", Action? onClick = null) {
            Severity = severity;
            Title = title;
            Message = message;
            ButtonText = buttonText;
            OnButtonClicked = onClick;

            OnPropertyChanged(nameof(ActionButtonVisibility));
        }

        // Commands

        [RelayCommand]
        private void ActionButtonClicked() {
            OnButtonClicked?.Invoke();
        }
    }
}
