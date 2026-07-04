using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using WinUiTemplate.Core.Stores;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Stores.Interfaces
{
    public interface IUserSettings
    {
        #region Settings Properties

        // Hidden
        bool Loaded { get; }
        bool IsFirstLaunch { get; set; }

        // Logging
        bool LogDebugMessages { get; set; }
        int MaxLogs { get; set; }

        // Appearance
        ThemeOption Theme { get; set; }
        BackdropOption Backdrop { get; set; }
        AccentSourceOption AccentSource { get; set; }
        Color CustomAccentColour { get; set; }
        WindowTintSourceOption WindowTintSource { get; set; }
        Color CustomWindowTintColour { get; set; }
        double WindowTintOpacity { get; set; }

        // Layout
        bool RememberLayout { get; set; }
        bool OpenMaximised { get; set; }
        int DefaultWidth { get; set; }
        int DefaultHeight { get; set; }

        // Backups
        string BackupsFolder { get; set; }
        int MaxBackups { get; set; }
        bool AutomaticBackups { get; set; }

        // API
        int ApiTimeout { get; set; }
        int ApiMaxRetries { get; set; }

        // Remote Database
        string DatabaseHost { get; set; }
        int DatabasePort { get; set; }
        string DatabaseName { get; set; }
        string DatabaseUsername { get; set; }
        string DatabasePassword { get; set; }
        int DatabaseConnectionTimeout { get; set; }

        // Search
        bool SearchCaseSensitive { get; set; }
        bool SearchSplitQuery { get; set; }

        #endregion

        // Events
        event Action? SettingsLoaded;
        event Action<string>? SettingChanged;

        // Public Functions
        Task Load();
        void RestoreDefaults();
    }
}
