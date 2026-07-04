using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
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
using WinUiTemplate.Core.Stores;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class ThemeService : IThemeService
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private DispatcherQueue uiThreadDispatcher;
        private UISettings uiSettings;
        private string[] appearanceSettings = {
            nameof(IUserSettings.Theme),
            nameof(IUserSettings.Backdrop),
            nameof(IUserSettings.AccentSource),
            nameof(IUserSettings.CustomAccentColour),
            nameof(IUserSettings.WindowTintSource),
            nameof(IUserSettings.CustomWindowTintColour),
            nameof(IUserSettings.WindowTintOpacity),
        };

        // Properties

        public bool DarkMode => userSettings.Theme == ThemeOption.Dark || Application.Current.RequestedTheme == ApplicationTheme.Dark;

        // Constructors

        public ThemeService(IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            userSettings.SettingChanged += OnSettingChanged;

            uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += OnColourValuesChanged;

            uiThreadDispatcher = DispatcherQueue.GetForCurrentThread();
        }

        // Events
        public event Action? ThemeChangeRequested;

        // Listeners

        private void OnSettingChanged(string name) {
            if (appearanceSettings.Contains(name)) ApplyTheme();
        }
        
        private void OnColourValuesChanged(UISettings sender, object args) {
            if (userSettings.Theme == ThemeOption.MatchWindows || 
                userSettings.AccentSource == AccentSourceOption.MatchWindows ||
                userSettings.WindowTintSource == WindowTintSourceOption.MatchWindows) {
                uiThreadDispatcher.TryEnqueue(RaiseThemeChangeRequested);
            }
        }

        // Public Functions

        public void ApplyTheme() {
            Color colour = userSettings.CustomAccentColour;
            if (userSettings.AccentSource == AccentSourceOption.MatchWindows) {
                colour = uiSettings.GetColorValue(UIColorType.AccentLight2);
            }
            // ToDo: experiment with non-monochrome accent
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
            ApplicationTheme windowsTheme = Application.Current.RequestedTheme;
            userSettings.Theme = DarkMode ? ThemeOption.Light : ThemeOption.Dark;
            RaiseThemeChangeRequested();
        }

        // Private Functions

        private void RaiseThemeChangeRequested() {
            ThemeChangeRequested?.Invoke();
        }
    }
}
