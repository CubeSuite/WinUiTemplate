using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services.Testing
{
    public class TestDialogService : IDialogService
    {
        public void Initialise(Window window) {
            // No-op for testing
        }

        public Task<StorageFile?> PickSaveLocation(string suggestedName, string extension, CancellationToken cancellationToken = default) {
            return Task.FromResult<StorageFile?>(null);
        }

        public Task<StorageFile?> PickSingleFile(string[]? filters = null, CancellationToken cancellationToken = default) {
            return Task.FromResult<StorageFile?>(null);
        }

        public Task<IReadOnlyList<StorageFile>> PickMultipleFiles(string[]? filters = null, CancellationToken cancellationToken = default) {
            return Task.FromResult<IReadOnlyList<StorageFile>>(Array.Empty<StorageFile>());
        }

        public Task<StorageFolder?> PickSingleFolder(CancellationToken cancellationToken = default) {
            return Task.FromResult<StorageFolder?>(null);
        }

        public Task ShowMessage(MessageType type, string title, string message = "", CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }

        public Task ShowMessage(DialogOptions options, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }

        public Task<bool> Confirm(string title, string message = "", CancellationToken cancellationToken = default) {
            return Task.FromResult(false);
        }

        public Task<bool> Confirm(DialogOptions options, CancellationToken cancellationToken = default) {
            return Task.FromResult(false);
        }

        public Task<ContentDialogResult> ShowDialog(ContentDialog dialog, CancellationToken cancellationToken = default) {
            return Task.FromResult(ContentDialogResult.None);
        }
    }
}
