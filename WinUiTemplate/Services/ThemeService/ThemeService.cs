using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class ThemeService : IThemeService
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Members
        private UISettings uiSettings = new UISettings();
        private string[] appearanceSettings = {
            nameof(IUserSettings.AccentColour),
            nameof(IUserSettings.Backdrop)
        };

        // Constructors
        public ThemeService(IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            userSettings.SettingChanged += OnSettingChanged;
        }

        // Events
        public event Action? ThemeChangeRequested;

        // Listeners

        private void OnSettingChanged(string name) {
            if (appearanceSettings.Contains(name)) ApplyTheme();
        }

        // Public Functions

        public void ApplyTheme() {
            Color colour = GetColour(userSettings.AccentColour, UIColorType.Accent);
            Application.Current.Resources["SystemAccentColor"] = colour;
            Application.Current.Resources["SystemAccentColorLight1"] = colour;
            Application.Current.Resources["SystemAccentColorLight2"] = colour;
            Application.Current.Resources["SystemAccentColorLight3"] = colour;
            Application.Current.Resources["SystemAccentColorDark1"] = colour;
            Application.Current.Resources["SystemAccentColorDark2"] = colour;
            Application.Current.Resources["SystemAccentColorDark3"] = colour;
            RaiseThemeChangeRequested();
        }

        public void ToggleTheme() {
            userSettings.DarkMode = !userSettings.DarkMode;
            RaiseThemeChangeRequested();
        }

        public void ResetAccentColour() {
            userSettings.AccentColour = uiSettings.GetColorValue(UIColorType.AccentLight2).ToHex();
        }

        // Private Functions

        private void RaiseThemeChangeRequested() {
            ThemeChangeRequested?.Invoke();
        }

        private Color GetColour(string hex, UIColorType fallbackType) {
            if (!string.IsNullOrWhiteSpace(hex)) {
                return hex.ToColor();
            }

            else return uiSettings.GetColorValue(fallbackType);
        }
    }
}
