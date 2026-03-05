using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.MVVM.Views.CustomTitleBar
{
    public partial class CustomTitleBarViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IProgramData programData;
        private readonly IUserSettings userSettings;
        private readonly IThemeService themeService;

        // Properties
        public string ProgramName => programData.ProgramName;
        public string ProgramVersion => $"V{programData.ProgramVersion.Major}.{programData.ProgramVersion.Minor}.{programData.ProgramVersion.Build}";
        public string ThemeIcon => userSettings.DarkMode ? "\uE706" : "\uE708";

        // Constructors
        public CustomTitleBarViewModel(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            themeService = serviceProvider.GetRequiredService<IThemeService>();
            themeService.ThemeChangeRequested += OnThemeChangeRequested;
        }

        // Listeners

        private void OnThemeChangeRequested() {
            OnPropertyChanged(nameof(ThemeIcon));
        }

        // Commands

        [RelayCommand]
        private void ToggleTheme() {
            themeService.ToggleTheme();
        }
    }
}
