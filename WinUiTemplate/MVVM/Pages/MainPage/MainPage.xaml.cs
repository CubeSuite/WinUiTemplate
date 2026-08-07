using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUiTemplate.Services;
using WinUiTemplate.Core.Services.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUiTemplate.MVVM.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    // Services & Stores
    IServiceProvider serviceProvider;
    INavigationService navigationService;

    // Fields
    private Type? currentPageViewModelType;

    public MainPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        this.serviceProvider = serviceProvider;
        navigationService = serviceProvider.GetRequiredService<INavigationService>();
        navigationService.NavigationRequested += OnNavigationRequested;
    }

    // Listeners

    private void OnNavigationRequested(ObservableObject pageViewModel) {
        if (pageFrame.DataContext is IDisposable disposableViewModel) {
            disposableViewModel.Dispose();
        }

        if(pageViewModel is HomePageViewModel homePageViewModel) {
            pageFrame.Navigate(typeof(HomePage));
        }
        else if (pageViewModel is BackupsPageViewModel backupsPageViewModel) {
            pageFrame.Navigate(typeof(BackupsPage));
        }
        else if (pageViewModel is SettingsPageViewModel settingsPageViewModel) {
            pageFrame.Navigate(typeof(SettingsPage));
        }

        currentPageViewModelType = pageViewModel.GetType();

        if (pageFrame.Content is FrameworkElement element) {
            element.DataContext = pageViewModel;
        }
    }

    private void OnNavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args) {
        if (args.IsSettingsInvoked) {
            NavigateIfNeeded(typeof(SettingsPageViewModel), () => new SettingsPageViewModel(serviceProvider));
            return;
        }

        string? tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag as string;
        switch (tag) {
            case "HomePage": NavigateIfNeeded(typeof(HomePageViewModel), () => new HomePageViewModel(serviceProvider)); break;
            case "Backups": NavigateIfNeeded(typeof(BackupsPageViewModel), () => new BackupsPageViewModel(serviceProvider)); break;
        }
    }

    // Private Functions

    private void NavigateIfNeeded(Type pageViewModelType, Func<ObservableObject> createViewModel) {
        if (currentPageViewModelType == pageViewModelType) return;
        navigationService.Navigate(createViewModel());
    }
}
