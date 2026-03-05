using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for managing application theme and backdrop settings.
    /// </summary>
    public interface IThemeService
    {
        // Events
        /// <summary>
        /// Occurs when a theme change is requested.
        /// </summary>
        event Action? ThemeChangeRequested;

        // Public Functions
        /// <summary>
        /// Applies the current theme settings to the application.
        /// </summary>
        void ApplyTheme();

        /// <summary>
        /// Toggles between light and dark theme modes.
        /// </summary>
        void ToggleTheme();

        /// <summary>
        /// Resets the accent color to the system default.
        /// </summary>
        void ResetAccentColour();

        // Enums

        public enum Backdrop {
            Acrylic,
            Mica
        }
    }
}
