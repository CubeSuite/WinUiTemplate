using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services
{
    public class NavigationService : INavigationService
    {
        // Events
        public event Action<ObservableObject>? NavigationRequested;

        // Properties
        public bool AllowNavigation { get; set; } = true;

        // Public Functions

        public void Navigate(ObservableObject pageViewModel) {
            if (AllowNavigation) NavigationRequested?.Invoke(pageViewModel);
        }
    }
}
