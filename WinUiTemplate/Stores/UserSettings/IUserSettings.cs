using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Stores.Interfaces
{
    public interface IUserSettings
    {
        // Properties
        bool Loaded { get; }
        bool IsFirstLaunch { get; set; }

        bool LogDebugMessages { get; set; }
        int MaxLogs { get; set; }
        
        bool DarkMode { get; set; }
        bool OpenMaximised { get; set; }
        IThemeService.Backdrop Backdrop { get; set; }
        string AccentColour { get; set; }

        string BackupsFolder { get; set; }
        int MaxBackups { get; set; }
        bool AutomaticBackups { get; set; }

        int ApiTimeout { get; set; }
        int ApiMaxRetries { get; set; }

        // Events
        event Action? SettingsLoaded;
        event Action<string>? SettingChanged;

        // Public Functions
        Task Load();
        void RestoreDefaults();
    }
}
