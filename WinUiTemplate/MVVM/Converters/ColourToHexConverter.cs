using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace WinUiTemplate.MVVM.Converters
{
    internal class ColourToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex)) {
                try {
                    return hex.ToColor();
                }
                catch {
                    return Color.FromArgb(0, 0, 0, 0);
                }
            }

            return Color.FromArgb(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return value is Color colour ? colour.ToHex() : DependencyProperty.UnsetValue;
        }
    }
}
