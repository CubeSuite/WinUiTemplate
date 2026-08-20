using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.MVVM.Views.CustomTitleBar
{
    public partial class CustomTitleBarViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IProgramData programData;
        private readonly IThemeService themeService;
        private readonly IUserSettings userSettings;
        private readonly Dictionary<string, LanguageOption> languageOptions;

        // Fields
        private bool isLanguageSelectorVisible;
        private bool isLanguageDropdownOpen;

        // Properties
        public string ProgramName => programData.ProgramName;
        public string ProgramVersion => $"V{programData.ProgramVersion.Major}.{programData.ProgramVersion.Minor}.{programData.ProgramVersion.Build}";
        public string ThemeIcon => themeService.DarkMode ? "\uE706" : "\uE708";
        public string[] LanguageOptions { get; }
        public bool IsLanguageButtonVisible => userSettings.EasyLanguageSwitching && !isLanguageSelectorVisible;
        public bool IsLanguageSelectorVisible => userSettings.EasyLanguageSwitching && isLanguageSelectorVisible;
        public bool IsLanguageDropdownOpen {
            get => isLanguageDropdownOpen;
            set {
                if (isLanguageDropdownOpen == value) return;

                isLanguageDropdownOpen = value;
                OnPropertyChanged();

                if (!value) CloseLanguageSelector();
            }
        }
        public string SelectedLanguage {
            get => userSettings.Language.GetDescription();
            set {
                if (languageOptions.TryGetValue(value, out LanguageOption language)) {
                    _ = userSettings.ChangeLanguageAsync(language);
                    CloseLanguageSelector();
                }
            }
        }

        // Constructors
        public CustomTitleBarViewModel(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
            themeService = serviceProvider.GetRequiredService<IThemeService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            languageOptions = EnumExtensions.GetValuesWithDescriptions<LanguageOption>();
            LanguageOptions = languageOptions.Keys.ToArray();
            themeService.ThemeChangeRequested += OnThemeChangeRequested;
            userSettings.SettingChanged += OnSettingChanged;
        }

        // Listeners

        private void OnThemeChangeRequested() {
            OnPropertyChanged(nameof(ThemeIcon));
        }

        private void OnSettingChanged(string settingName) {
            if (settingName != nameof(IUserSettings.EasyLanguageSwitching)) return;

            OnPropertyChanged(nameof(IsLanguageButtonVisible));
            OnPropertyChanged(nameof(IsLanguageSelectorVisible));
        }

        // Commands

        [RelayCommand]
        private void ToggleTheme() {
            themeService.ToggleTheme();
        }

        [RelayCommand]
        private void OpenLanguageSelector() {
            isLanguageSelectorVisible = true;
            isLanguageDropdownOpen = true;
            OnPropertyChanged(nameof(IsLanguageSelectorVisible));
            OnPropertyChanged(nameof(IsLanguageButtonVisible));
            OnPropertyChanged(nameof(IsLanguageDropdownOpen));
        }

        public void Dispose() {
            themeService.ThemeChangeRequested -= OnThemeChangeRequested;
            userSettings.SettingChanged -= OnSettingChanged;
        }

        [RelayCommand]
        private void CloseLanguageSelector() {
            isLanguageSelectorVisible = false;
            isLanguageDropdownOpen = false;
            OnPropertyChanged(nameof(IsLanguageSelectorVisible));
            OnPropertyChanged(nameof(IsLanguageButtonVisible));
            OnPropertyChanged(nameof(IsLanguageDropdownOpen));
        }
    }
}
