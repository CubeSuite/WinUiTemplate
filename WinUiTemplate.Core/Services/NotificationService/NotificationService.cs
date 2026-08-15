using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.MVVM.Models.ViewModels;

namespace WinUiTemplate.Core.Services
{
    public class NotificationService : INotificationService
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Fields
        private readonly object subscriberLock = new object();
        private readonly Queue<NotificationViewModel> pendingNotifications = new Queue<NotificationViewModel>();
        private Action<NotificationViewModel>? notificationRequested;

        // Constructors

        public NotificationService(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Events
        public event Action<NotificationViewModel>? NotificationRequested {
            add {
                if (value == null) return;

                List<NotificationViewModel> backlog;
                lock (subscriberLock) {
                    notificationRequested += value;
                    backlog = pendingNotifications.ToList();
                    pendingNotifications.Clear();
                }

                foreach (NotificationViewModel notification in backlog) {
                    value.Invoke(notification);
                }
            }
            remove {
                lock (subscriberLock) {
                    notificationRequested -= value;
                }
            }
        }

        // Public Functions

        public void Notify(InfoBarSeverity level, string title, string message = "", string buttonText = "", Action? onClick = null) {
            if(title == null) {
                Debug.Assert(false, "NotificationService.Notify failed: 'title was null'");
                return;
            }

            LogNotification(level, title, message);

            NotificationViewModel notification = new NotificationViewModel(level, title, message, buttonText, onClick);

            lock (subscriberLock) {
                if (notificationRequested == null) {
                    pendingNotifications.Enqueue(notification);
                    return;
                }
            }

            notificationRequested?.Invoke(notification);
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
