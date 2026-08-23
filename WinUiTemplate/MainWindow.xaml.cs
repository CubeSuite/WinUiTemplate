using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinUiTemplate.MVVM.Pages;
using WinUiTemplate.MVVM.Views.CustomTitleBar;
using WinUiTemplate.Core.Services;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI;
using Windows.Foundation;
using CommunityToolkit.WinUI.Helpers;

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
        private readonly ILanguageService lang;
        private readonly IBackupService backupService;
        private readonly IDialogService dialogService;
        private readonly IUserSettings userSettings;
        private readonly IThemeService themeService;

        // Fields
        private bool isClosing = false;
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

            lang = new LanguageService("MainWindow");

            userSettings.SettingsLoaded += OnSettingsLoaded;
            themeService.ThemeChangeRequested += OnThemeChangeRequested;

            AppWindow.Closing += OnMainWindowClosing;

            OnSettingsLoaded();
        }

        // Listeners

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args) {
            if (acrylicConfiguration != null) {
                acrylicConfiguration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private async void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (isClosing) return;
            
            isClosing = true;
            args.Cancel = true;

            if (userSettings.RememberLayout) {
                userSettings.DefaultWidth = sender.Size.Width;
                userSettings.DefaultHeight = sender.Size.Height;

                if (sender.Presenter is OverlappedPresenter presenter) {
                    userSettings.OpenMaximised = presenter.State == OverlappedPresenterState.Maximized;
                }
            }

            userSettings.IsFirstLaunch = false;

            await userSettings.SaveNowAsync();

            OperationResult result = await backupService.CreateBackupAsync();
            if (!result.Success && result.Notify) {
                await dialogService.ShowMessage(MessageType.Warning, lang.Get("BackupFailed"), result.ErrorMessage ?? "");                
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
                notificationService.Notify(InfoBarSeverity.Warning, lang.Get("AcrylicWarningTitle"), lang.Get("AcrylicWarningMessage"));
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
                notificationService.Notify(InfoBarSeverity.Warning, lang.Get("AcrylicWarningTitle"), lang.Get("AcrylicWarningMessage"));
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
                WindowTintSourceOption.Solid => new SolidColorBrush(userSettings.SolidWindowTintColour),
                WindowTintSourceOption.Gradient => GetGradient(),
                WindowTintSourceOption.MatchAccent => new SolidColorBrush(userSettings.CustomAccentColour),
                WindowTintSourceOption.MatchWindows => new SolidColorBrush(new UISettings().GetColorValue(UIColorType.AccentLight2)),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            EnsureNoiseOverlay();
        }

        private LinearGradientBrush GetGradient() {
            return userSettings.GradientPreset switch {
                GradientOption.Acid => FormGradientFromColours("#CBFF00".ToColor(), "#0C1100".ToColor()),
                GradientOption.Aqua => FormGradientFromColours("#22D3EE".ToColor(), "#0F766E".ToColor()),
                GradientOption.Aurora => FormGradientFromColours("#00D4FF".ToColor(), "#5B0FBE".ToColor()),
                GradientOption.BerryGlow => FormGradientFromColours("#FB7185".ToColor(), "#9333EA".ToColor()),
                GradientOption.Crimson => FormGradientFromColours("#FF0054".ToColor(), "#130008".ToColor()),
                GradientOption.EmeraldDeep => FormGradientFromColours("#00E68A".ToColor(), "#005F73".ToColor()),
                GradientOption.Glacier => FormGradientFromColours("#E8F4F8".ToColor(), "#0D3B5E".ToColor()),
                GradientOption.LilacDusk => FormGradientFromColours("#D8E1F5".ToColor(), "#7B5EA7".ToColor()),
                GradientOption.Pulse => FormGradientFromColours("#7000FF".ToColor(), "#07001A".ToColor()),
                GradientOption.SageBlush => FormGradientFromColours("#BEE8E0".ToColor(), "#C97B8A".ToColor()),
                GradientOption.Sangria => FormGradientFromColours("#C0392B".ToColor(), "#080205".ToColor()),
                GradientOption.Tide => FormGradientFromColours("#00D4FF".ToColor(), "#001419".ToColor()),
                GradientOption.Twilight => FormGradientFromColours("#FF9A8B".ToColor(), "#1E1040".ToColor()),
                GradientOption.VioletVolt => FormGradientFromColours("#2E9EFF".ToColor(), "#FF3D7A".ToColor()),
                GradientOption.Custom => FormGradientFromColours(null, null),
                _ => FormGradientFromColours(null, null),
            };
        }

        private LinearGradientBrush FormGradientFromColours(Color? start, Color? end) {
            Point startPoint = new Point(userSettings.GradientStart.PositionX, userSettings.GradientStart.PositionY);
            Point endPoint = new Point(userSettings.GradientEnd.PositionX, userSettings.GradientEnd.PositionY);

            Color startColour = start ?? userSettings.GradientStart.Colour.ToColor();
            Color endColour = end ?? userSettings.GradientEnd.Colour.ToColor();

            if (start != null) userSettings.GradientStart.Colour = startColour.ToHex();
            if (end != null) userSettings.GradientEnd.Colour = endColour.ToHex();

            return new LinearGradientBrush() {
                StartPoint = startPoint,
                EndPoint = endPoint,
                GradientStops = CreateBandingReducedStops(startColour, endColour, userSettings.GradientStart.Offset, userSettings.GradientEnd.Offset, 10)
            };
        }

        private GradientStopCollection CreateBandingReducedStops(Color startColour, Color endColour, double startOffset, double endOffset, int stopCount) {
            stopCount = Math.Max(stopCount, 2);
            GradientStopCollection stops = new GradientStopCollection();

            for (int i = 0; i < stopCount; i++) {
                double progress = i / (double)(stopCount - 1);
                byte a = (byte)Math.Round(startColour.A + ((endColour.A - startColour.A) * progress));
                byte r = (byte)Math.Round(startColour.R + ((endColour.R - startColour.R) * progress));
                byte g = (byte)Math.Round(startColour.G + ((endColour.G - startColour.G) * progress));
                byte b = (byte)Math.Round(startColour.B + ((endColour.B - startColour.B) * progress));
                
                stops.Add(new GradientStop() {
                    Color = Color.FromArgb(a, r, g, b),
                    Offset = startOffset + ((endOffset - startOffset) * progress)
                });
            }

            return stops;
        }

        private void EnsureNoiseOverlay() {
            if (NoiseOverlay.Source != null) return;

            const int size = 128;
            byte[] pixels = new byte[size * size * 4];
            Random random = new Random(42);

            for (int i = 0; i < pixels.Length; i += 4) {
                byte value = (byte)random.Next(110, 146);
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }

            WriteableBitmap bitmap = new WriteableBitmap(size, size);
            using (Stream pixelStream = bitmap.PixelBuffer.AsStream()) {
                pixelStream.Write(pixels, 0, pixels.Length);
            }

            NoiseOverlay.Source = bitmap;
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
