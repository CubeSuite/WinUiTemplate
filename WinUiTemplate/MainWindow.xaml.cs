using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Helpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT;
using WinUiTemplate.MVVM.Pages;
using WinUiTemplate.MVVM.Views.CustomTitleBar;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUiTemplate
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IUserSettings userSettings;
        private readonly IBackupService backupService;
        private readonly IDialogService dialogService;
        private readonly IThemeService themeService;
        private readonly INavigationService navigationService;

        // Constructors

        public MainWindow(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            backupService = serviceProvider.GetRequiredService<IBackupService>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            themeService = serviceProvider.GetRequiredService<IThemeService>();
            navigationService = serviceProvider.GetRequiredService<INavigationService>();

            userSettings.SettingsLoaded += OnSettingsLoaded;
            themeService.ThemeChangeRequested += OnThemeChangeRequested;

            AppWindow.Closing += OnMainWindowClosing;
        }

        // Listeners

        private void OnSettingsLoaded() {
            InitializeComponent();
            ConfigureAppearance();

            if (userSettings.IsFirstLaunch) {
                userSettings.DarkMode = ((FrameworkElement)Content).RequestedTheme != ElementTheme.Light;
                themeService.ResetAccentColour();
            }
            else {
                themeService.ApplyTheme();
            }

            InitialiseMainPage();
        }

        private void OnThemeChangeRequested() {
            ApplyTheme();
        }

        private async void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (args.Cancel) return;
            args.Cancel = true;

            userSettings.IsFirstLaunch = false;
            OperationResult result = await backupService.CreateBackupAsync();
            if (!result.Success && result.Notify) {
                await dialogService.ShowMessage(MessageType.Warning, "Backup Failed", result.ErrorMessage ?? "");                
            }

            sender.Closing -= OnMainWindowClosing;
            Close();
        }

        // Private Functions

        private void ApplyTheme() {
            FrameworkElement? root = Content as FrameworkElement;
            if (root == null) return;

            SystemBackdrop = userSettings.Backdrop == IThemeService.Backdrop.Acrylic ? new DesktopAcrylicBackdrop() : new MicaBackdrop();

            ForceAccentUpdate(root);

            bool dark = userSettings.DarkMode;
            AppWindow.TitleBar.PreferredTheme = dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
            root.RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
        }

        private void ForceAccentUpdate(FrameworkElement root) {
            root.RequestedTheme = userSettings.DarkMode ? ElementTheme.Light : ElementTheme.Dark;
            root.RequestedTheme = userSettings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
        }

        private void ConfigureAppearance() {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 900));
            ExtendsContentIntoTitleBar = true;
            titleBar.DataContext = new CustomTitleBarViewModel(serviceProvider);
            SetTitleBar(titleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

            if (userSettings.OpenMaximised && AppWindow.Presenter is OverlappedPresenter presenter) {
                presenter.Maximize();
            }

            Activated += (sender, e) => {
                rootGrid.Focus(FocusState.Programmatic);
            };
        }

        private void InitialiseMainPage() {
            MainPage mainPage = new MainPage(serviceProvider);
            mainPage.DataContext = new MainPageViewModel(serviceProvider);

            navigationService.Navigate(new HomePageViewModel(serviceProvider));

            mainPage.SetValue(Grid.RowProperty, 1);
            rootGrid.Children.Add(mainPage);
        }
    }
}
