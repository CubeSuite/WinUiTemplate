using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for navigating between pages in the application.
    /// </summary>
    public interface INavigationService
    {
        // Properties
        public bool AllowNavigation { get; set; }

        // Events
        /// <summary>
        /// Occurs when navigation to a page is requested.
        /// </summary>
        event Action<ObservableObject>? NavigationRequested;

        // Public Functions
        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="pageViewModel">The view model of the page to navigate to.</param>
        void Navigate(ObservableObject pageViewModel);
    }
}
