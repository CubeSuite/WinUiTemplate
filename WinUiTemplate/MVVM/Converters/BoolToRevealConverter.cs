using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace WinUiTemplate.MVVM.Converters
{
    public class BoolToRevealConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool valueAsBool && valueAsBool) return PasswordRevealMode.Visible;
            return PasswordRevealMode.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is PasswordRevealMode mode) return mode == PasswordRevealMode.Visible;
            throw new NotImplementedException();
        }
    }
}
