using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.MVVM.ViewModels;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services
{
    public class NotificationService : INotificationService
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Constructors

        public NotificationService(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Events
        public event Action<NotificationViewModel>? NotificationRequested;

        // Public Functions

        public void Notify(InfoBarSeverity level, string title, string message = "", string buttonText = "", Action? onClick = null) {
            if(title == null) {
                Debug.Assert(false, "NotificationService.Notify failed: 'title was null'");
                return;
            }

            LogNotification(level, title, message);
            NotificationRequested?.Invoke(new NotificationViewModel(level, title, message, buttonText, onClick));
        }

        // Private Functions
        private void LogNotification(InfoBarSeverity level, string title, string message) {
            try {
                string entry = $"Notification Displayed: {level} | {title} - {message}";
                switch (level) {
                    case InfoBarSeverity.Informational:
                    case InfoBarSeverity.Success: logger.LogInfo(entry); break;
                    case InfoBarSeverity.Warning: logger.LogWarning(entry); break;
                    case InfoBarSeverity.Error: logger.LogError(entry); break;
                }
            }
            catch (Exception e) {
                Debug.Assert(false, $"NotificationService.LogNotification failed: '{e.Message}'");
            }
        }
    }
}
