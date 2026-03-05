using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using WinUiTemplate.Services;

namespace WinUiTemplate.MVVM.Views.MessageView
{
    public class MessageViewModel
    {
        // Properties
        public string Icon { get; }
        public string Title { get; }
        public string Message { get; }
        public SolidColorBrush IconForeground { get; }
        public SolidColorBrush HeaderBackground { get; }

        // Constructors

        public MessageViewModel(MessageType type, string title, string message) {
            Title = title;
            Message = message;

            Icon = type switch {
                MessageType.None => "\uE897",
                MessageType.Info => "\uE946",
                MessageType.Warning => "\uE7BA",
                MessageType.Error => "\uEA39",
                MessageType.Success => "\uE73E",
                _ => ""
            };

            Color colour = type switch {
                MessageType.None => Colors.DodgerBlue,
                MessageType.Info => Colors.DodgerBlue,
                MessageType.Warning => Colors.DarkOrange,
                MessageType.Error => Colors.IndianRed,
                MessageType.Success => Color.FromArgb(255, 16, 124, 16),
                _ => Colors.Black
            };

            IconForeground = new SolidColorBrush(colour);
            HeaderBackground = new SolidColorBrush(Color.FromArgb(40, colour.R, colour.G, colour.B));
        }
    }
}
