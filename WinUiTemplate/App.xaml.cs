using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Web.Http;
using WinUiTemplate.MVVM.Pages;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Services.Testing;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUiTemplate
{
    // ToDo: Unit Tests

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        // Services & Stores
        private IServiceProvider serviceProvider = null!;
        private ILoggerService logger = null!;
        private IProgramData programData = null!;
        private IUserSettings userSettings = null!;
        private INotificationService notificationService = null!;
        private IFileUtils fileUtils = null!;

        // Members
        private Window? _window;

        // Testing
        const bool testArchiveService = false;
        const bool testEncryptionService = false;
        const bool testFileUtils = false;
        const bool testBackupService = false;

        // Constructors

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureServiceProvider();
            InitializeComponent();

            UnhandledException += OnUnhandledException;
        }

        // Listeners

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            MainWindow mainWindow = new MainWindow(serviceProvider);
            _window = mainWindow;
            _window.Activate();

            await InitialiseServices();
            InitialiseUiServices(mainWindow);
            logger?.LogInfo("Initialised UI services");

            if (programData == null || userSettings == null) return;
            if (programData.EnableBackups && userSettings.AutomaticBackups && 
                string.IsNullOrWhiteSpace(userSettings.BackupsFolder)) {
                notificationService?.Notify(InfoBarSeverity.Warning, "Automatic Backups Disabled", "Automatic Backups will not work until you choose a location to store them in the settings.");
            }

            await fileUtils.TryWriteFileAsync($"{programData.FilePaths.DataFolder}\\Test.txt", "Some random text", true);
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
            string timestamp = fileUtils.GetFileSafeTimestamp();
            string path = Path.Combine(programData.FilePaths.CrashReportsFolder, $"{timestamp}.log");
            string crashlog = $"{e.Message}\n\n{e.Exception.StackTrace}";
            fileUtils.TryWriteFileAsync(path, crashlog);
        }

        // Private Functions

        private void ConfigureServiceProvider() {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton(typeof(IArchiveService), testArchiveService ? typeof(TestArchiveService) : typeof(ArchiveService));
            services.AddSingleton(typeof(IBackupService), testBackupService ? typeof(TestBackupService) : typeof(BackupService));
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton(typeof(IEncryptionService), testEncryptionService ? typeof(TestEncryptionService) : typeof(EncryptionService));
            services.AddSingleton<IHttpService, HttpService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IThemeService, ThemeService>();

            services.AddSingleton<IProgramData, ProgramData>();
            services.AddSingleton<IUserSettings, UserSettings>();

            services.AddSingleton(typeof(IFileUtils), testFileUtils ? typeof(TestFileUtils) : typeof(FileUtils));

            serviceProvider = services.BuildServiceProvider();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            programData = serviceProvider.GetRequiredService<IProgramData>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
        }

        private async Task InitialiseServices() {
            await serviceProvider.GetRequiredService<IFileUtils>().CreateProgramFolderStructure();
            await serviceProvider.GetRequiredService<IUserSettings>().Load();
        }

        private void InitialiseUiServices(MainWindow mainWindow) {
            serviceProvider.GetRequiredService<IDialogService>().Initialise(mainWindow);
        }
    }
}
