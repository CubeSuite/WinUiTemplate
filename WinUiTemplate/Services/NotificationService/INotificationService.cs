using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.MVVM.ViewModels;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for displaying in-app notifications.
    /// </summary>
    public interface INotificationService
    {
        // Events
        /// <summary>
        /// Occurs when a notification is requested to be displayed.
        /// </summary>
        event Action<NotificationViewModel>? NotificationRequested;

        // Public Functions
        /// <summary>
        /// Displays an in-app notification.
        /// </summary>
        /// <param name="level">The severity level of the notification.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The message content to display.</param>
        void Notify(InfoBarSeverity level, string title, string message = "", string buttonText = "", Action? onClick = null);
    }
}
