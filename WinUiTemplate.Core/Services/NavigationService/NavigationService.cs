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
        public event Action<bool>? AllowNavigationChanged;

        // Members
        private bool _allowNavigation = true;

        // Properties
        
        public bool AllowNavigation {
            get => _allowNavigation;
            set {
                if (_allowNavigation == value) return;
                _allowNavigation = value;
                AllowNavigationChanged?.Invoke(value);
            }
        }

        // Public Functions

        public void Navigate(ObservableObject pageViewModel) {
            if (AllowNavigation) NavigationRequested?.Invoke(pageViewModel);
        }
    }
}
