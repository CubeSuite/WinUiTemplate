using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Stores.Interfaces
{
    public enum ThemeOption {
        Light,
        Dark,
        [Description("Match Windows")] MatchWindows
    }

    public enum BackdropOption {
        Mica,
        [Description("Tinted Mica")] MicaAlt,
        [Description("Acrylic")] AcrylicBase,
        [Description("Thin Acrylic")] AcrylicThin
    }

    public enum AccentSourceOption {
        [Description("Match Windows")] MatchWindows,
        Custom
    }

    public enum WindowTintSourceOption {
        None,
        Custom,
        [Description("Match Accent")] MatchAccent,
        [Description("Match Windows")] MatchWindows
    }

    public static class EnumExtensions 
    {
        public static string GetDescription<T>(this T enumValue) where T : Enum {
            FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? enumValue.ToString();
        }

        public static Dictionary<string, T> GetValuesWithDescriptions<T>() where T : struct, Enum {
            return Enum.GetValues<T>().ToDictionary(value => value.GetDescription(), value => value);
        }
    }
}
