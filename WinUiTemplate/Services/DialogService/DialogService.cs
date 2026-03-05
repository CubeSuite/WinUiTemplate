using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input.ForceFeedback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using WinUiTemplate.MVVM.Views.MessageView;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services
{
    public class DialogService : IDialogService
    {
        // Members
        private nint windowId;
        private XamlRoot? xamlRoot;
        private readonly SemaphoreSlim dialogLock = new SemaphoreSlim(1, 1);

        // Properties
        private bool Initialised => xamlRoot != null;

        // Public Functions

        public void Initialise(Window window) {
            windowId = WindowNative.GetWindowHandle(window);

            if(window.Content is FrameworkElement root) {
                root.Loaded += async (sender, e) => {
                    xamlRoot = root.XamlRoot;
                    await Task.CompletedTask;
                };
            }
            else {
                Debug.Assert(false, "DialogService.Initialise called with invalid window content");
            }
        }

        public async Task ShowMessage(MessageType type, string title, string message = "", CancellationToken cancellationToken = default) {
            await ShowMessage(new DialogOptions(
                Type: type,
                Title: title,
                Message: message,
                PrimaryText: "",
                SecondaryText: "",
                CloseText: "Close",
                DefaultButton: ContentDialogButton.Close)
             );
        }

        public async Task ShowMessage(DialogOptions options, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return;

            MessageView messageView = new MessageView() {
                DataContext = new MessageViewModel(
                    type: options.Type,
                    title: options.Title,
                    message: options.Message
                )
            };

            ContentDialog dialog = new ContentDialog() {
                XamlRoot = xamlRoot,
                Content = messageView,
                PrimaryButtonText = options.PrimaryText,
                SecondaryButtonText = options.SecondaryText,
                CloseButtonText = options.CloseText,
                DefaultButton = options.DefaultButton
            };

            await ShowDialog(dialog, cancellationToken);
        }

        public async Task<bool> Confirm(string title, string message = "", CancellationToken cancellationToken = default) {
            return await Confirm(new DialogOptions(
                Type: MessageType.Info,
                Title: title,
                Message: message,
                PrimaryText: "Yes",
                SecondaryText: "No",
                CloseText: "",
                DefaultButton: ContentDialogButton.Primary
            ), cancellationToken);
        }

        public async Task<bool> Confirm(DialogOptions options, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return false;

            MessageView messageView = new MessageView() {
                DataContext = new MessageViewModel(
                    type: MessageType.None,
                    title: options.Title,
                    message: options.Message
                )
            };

            ContentDialog dialog = new ContentDialog() {
                XamlRoot = xamlRoot,
                Content = messageView,
                PrimaryButtonText = options.PrimaryText ?? "Yes",
                SecondaryButtonText = options.SecondaryText ?? "No",
                CloseButtonText = "",
                DefaultButton = options.DefaultButton
            };

            ContentDialogResult result = await ShowDialog(dialog, cancellationToken);
            return result == ContentDialogResult.Primary;
        }

        public async Task<ContentDialogResult> ShowDialog(ContentDialog dialog, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) {
                Debug.Assert(false, "Show dialog called before DialogService was initialised");
                return ContentDialogResult.None;
            }

            dialog.XamlRoot ??= xamlRoot;

            await dialogLock.WaitAsync(cancellationToken);
            try {
                using (cancellationToken.Register(() => {
                    try { dialog.Hide(); } catch {}
                })) {
                    return await dialog.ShowAsync().AsTask(cancellationToken);
                }
            }
            catch {
                return ContentDialogResult.None;
            }
            finally {
                dialogLock.Release();
            }
        }

        public async Task<StorageFile?> PickSaveLocation(string suggestedName, string extension, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return null;

            try {
                FileSavePicker picker = new FileSavePicker() {
                    SuggestedFileName = suggestedName,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };

                picker.FileTypeChoices.Add("Zip Archive", new List<string> { extension });

                InitializeWithWindow.Initialize(picker, windowId);
                return await picker.PickSaveFileAsync();
            }
            catch {
                return null;
            }
        }

        public async Task<StorageFile?> PickSingleFile(string[]? filters = null, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return null;

            try {
                FileOpenPicker picker = new FileOpenPicker() {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };

                if (filters == null || filters.Length == 0) filters = ["*"];
                foreach(string filter in filters.Distinct()) {
                    picker.FileTypeFilter.Add(filter);
                }

                InitializeWithWindow.Initialize(picker, windowId);
                return await picker.PickSingleFileAsync().AsTask(cancellationToken);
            }
            catch {
                return null;
            }
        }

        public async Task<IReadOnlyList<StorageFile>> PickMultipleFiles(string[]? filters = null, CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return Array.Empty<StorageFile>();

            try {
                FileOpenPicker picker = new FileOpenPicker() {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };

                if (filters == null || filters.Count() == 0) filters = ["*"];
                foreach(string filter in filters) {
                    picker.FileTypeFilter.Add(filter);
                }

                InitializeWithWindow.Initialize(picker, windowId);
                return await picker.PickMultipleFilesAsync().AsTask(cancellationToken) ?? Array.Empty<StorageFile>();
            }
            catch {
                return Array.Empty<StorageFile>();
            }
        }

        public async Task<StorageFolder?> PickSingleFolder(CancellationToken cancellationToken = default) {
            if (!EnsureInitialised()) return null;

            try {
                FolderPicker picker = new FolderPicker() {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };

                picker.FileTypeFilter.Add("*");

                InitializeWithWindow.Initialize(picker, windowId);
                return await picker.PickSingleFolderAsync().AsTask(cancellationToken);
            }
            catch {
                return null;
            }
        }

        // Private Functions

        private bool EnsureInitialised() {
            if (Initialised) return true;

            Debug.Assert(false, "DialogService used before initialisation(XamlRoot is null)");
            return false;
        }
    }
}
