using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Helpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
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
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT;
using WinUiTemplate.Core.Stores;
using WinUiTemplate.MVVM.Pages;
using WinUiTemplate.MVVM.Views.CustomTitleBar;
using WinUiTemplate.Services;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

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
        private readonly INotificationService notificationService;
        private readonly INavigationService navigationService;
        private readonly IBackupService backupService;
        private readonly IDialogService dialogService;
        private readonly IUserSettings userSettings;
        private readonly IThemeService themeService;

        // Fields
        private DesktopAcrylicController? acrylicController;
        private SystemBackdropConfiguration? acrylicConfiguration;
        private FrameworkElement? acrylicThemeRoot;

        // Constructors

        public MainWindow(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            navigationService = serviceProvider.GetRequiredService<INavigationService>();
            backupService = serviceProvider.GetRequiredService<IBackupService>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            themeService = serviceProvider.GetRequiredService<IThemeService>();

            userSettings.SettingsLoaded += OnSettingsLoaded;
            themeService.ThemeChangeRequested += OnThemeChangeRequested;

            AppWindow.Closing += OnMainWindowClosing;
        }

        // Listeners

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args) {
            if (acrylicConfiguration != null) {
                acrylicConfiguration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private async void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (args.Cancel) return;
            args.Cancel = true;

            if (userSettings.RememberLayout) {
                userSettings.DefaultWidth = sender.Size.Width;
                userSettings.DefaultHeight = sender.Size.Height;

                if (sender.Presenter is OverlappedPresenter presenter) {
                    userSettings.OpenMaximised = presenter.State == OverlappedPresenterState.Maximized;
                }
            }

            userSettings.IsFirstLaunch = false;
            OperationResult result = await backupService.CreateBackupAsync();
            if (!result.Success && result.Notify) {
                await dialogService.ShowMessage(MessageType.Warning, "Backup Failed", result.ErrorMessage ?? "");                
            }

            DisposeAcrylicBackdrop();
            sender.Closing -= OnMainWindowClosing;
            Close();
        }

        private void OnSettingsLoaded() {
            InitializeComponent();
            ConfigureLayout();
            themeService.ApplyTheme();
            InitialiseMainPage();
        }

        private void OnThemeChangeRequested() {
            ApplyTheme();
        }
        
        private void OnAcrylicThemeChanged(FrameworkElement sender, object args) {
            UpdateAcrylicTheme();
        }

        // Private Functions
        private void ConfigureLayout() {
            int width = userSettings.DefaultWidth != 0 ? userSettings.DefaultWidth : 1600;
            int height = userSettings.DefaultHeight != 0 ? userSettings.DefaultHeight : 900;
            AppWindow.Resize(new SizeInt32(width, height));

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

        private void ApplyTheme() {
            FrameworkElement? root = Content as FrameworkElement;
            if (root == null) return;

            switch (userSettings.Backdrop) {
                case BackdropOption.Mica:
                    DisposeAcrylicBackdrop();
                    SystemBackdrop = new MicaBackdrop();
                    break;

                case BackdropOption.MicaAlt:
                    DisposeAcrylicBackdrop();
                    SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
                    break;

                case BackdropOption.AcrylicThin:
                    SetAcrylicBackdrop(useThin: true, root);
                    break;

                case BackdropOption.AcrylicBase:
                default:
                    SetAcrylicBackdrop(useThin: false, root);
                    break;
            }

            ForceAccentUpdate(root);

            AppWindow.TitleBar.PreferredTheme = themeService.DarkMode ? TitleBarTheme.Dark: TitleBarTheme.Light;
            UpdateAcrylicTheme();
            ApplyWindowTint();
        }

        private void SetAcrylicBackdrop(bool useThin, FrameworkElement root) {
            DisposeAcrylicBackdrop();
            
            if (!DesktopAcrylicController.IsSupported()) {
                notificationService.Notify(InfoBarSeverity.Warning, "Desktop Acrylic is not supported on this system", "Switching to Mica backdrop.");
                SystemBackdrop = new MicaBackdrop();
                return;
            }

            SystemBackdrop = null;
            acrylicThemeRoot = root;
            acrylicThemeRoot.ActualThemeChanged += OnAcrylicThemeChanged;
            Activated += OnWindowActivated;

            acrylicConfiguration = new SystemBackdropConfiguration() {
                IsInputActive = true
            };

            acrylicController = new DesktopAcrylicController() {
                Kind = useThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
            };

            object backdropHost = this;
            if (backdropHost is not ICompositionSupportsSystemBackdrop backdropTarget) {
                notificationService.Notify(InfoBarSeverity.Warning, "Desktop Acrylic is not supported on this system", "Switching to Mica backdrop.");
                SystemBackdrop = new MicaBackdrop();
                return;
            }

            acrylicController.AddSystemBackdropTarget(backdropTarget);
            acrylicController.SetSystemBackdropConfiguration(acrylicConfiguration);

            UpdateAcrylicTheme();
        }

        private void UpdateAcrylicTheme() {
            if (acrylicConfiguration == null || acrylicThemeRoot == null) return;

            acrylicConfiguration.Theme = acrylicThemeRoot.ActualTheme switch {
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                _ => SystemBackdropTheme.Default
            };
        }

        private void ApplyWindowTint() {
            Tint.Opacity = Math.Clamp(userSettings.WindowTintOpacity, 0, 1);
            Tint.Fill = userSettings.WindowTintSource switch {
                WindowTintSourceOption.None => new SolidColorBrush(Colors.Transparent),
                WindowTintSourceOption.Custom => new SolidColorBrush(userSettings.CustomWindowTintColour),
                WindowTintSourceOption.MatchAccent => new SolidColorBrush(userSettings.CustomAccentColour),
                WindowTintSourceOption.MatchWindows => new SolidColorBrush(new UISettings().GetColorValue(UIColorType.AccentLight2)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }

        private void DisposeAcrylicBackdrop() {
            if (acrylicThemeRoot != null) {
                acrylicThemeRoot.ActualThemeChanged -= OnAcrylicThemeChanged;
                acrylicThemeRoot = null;
            }

            Activated -= OnWindowActivated;

            acrylicController?.Dispose();
            acrylicController = null;
            acrylicConfiguration = null;
        }

        private void ForceAccentUpdate(FrameworkElement root) {
            root.RequestedTheme = themeService.DarkMode ? ElementTheme.Light : ElementTheme.Dark;
            root.RequestedTheme = themeService.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
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
