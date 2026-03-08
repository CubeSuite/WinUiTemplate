using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for displaying dialogs and file pickers.
    /// </summary>
    public interface IDialogService
    {
        // Public Functions
        /// <summary>
        /// Initializes the dialog service with the specified window.
        /// </summary>
        /// <param name="window">The window that will host the dialogs.</param>
        public void Initialise(Window window);

        /// <summary>
        /// Displays a message dialog asynchronously.
        /// </summary>
        /// <param name="type">The type of message to display.</param>
        /// <param name="title">The title of the message dialog.</param>
        /// <param name="message">The message content to display.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task ShowMessage(MessageType type, string title, string message = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a message dialog with custom options asynchronously.
        /// </summary>
        /// <param name="options">The dialog options including title, message, and type.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task ShowMessage(DialogOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a confirmation dialog with Yes/No buttons asynchronously.
        /// </summary>
        /// <param name="title">The title of the confirmation dialog.</param>
        /// <param name="message">The message content to display.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>True if the user clicked Yes; false if the user clicked No.</returns>
        Task<bool> Confirm(string title, string message = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a confirmation dialog with Yes/No buttons and custom options asynchronously.
        /// </summary>
        /// <param name="options">The dialog options including title and message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>True if the user clicked Yes; false if the user clicked No.</returns>
        Task<bool> Confirm(DialogOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a custom content dialog asynchronously.
        /// </summary>
        /// <param name="dialog">The content dialog to display.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result indicating which button was pressed.</returns>
        Task<ContentDialogResult> ShowDialog(ContentDialog dialog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a file picker for selecting a save location asynchronously.
        /// </summary>
        /// <param name="suggestedName">The suggested file name.</param>
        /// <param name="extension">The file extension.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected storage file, or null if the user cancelled the operation.</returns>
        Task<StorageFile?> PickSaveLocation(string suggestedName, string extension, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a file picker for selecting a single file asynchronously.
        /// </summary>
        /// <param name="filters">Optional array of file type filters (e.g., ".txt", ".pdf").</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected storage file, or null if the user cancelled the operation.</returns>
        Task<StorageFile?> PickSingleFile(string[]? filters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a file picker for selecting multiple files asynchronously.
        /// </summary>
        /// <param name="filters">Optional array of file type filters (e.g., ".txt", ".pdf").</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A read-only list of selected storage files, or an empty list if the user cancelled the operation.</returns>
        Task<IReadOnlyList<StorageFile>> PickMultipleFiles(string[]? filters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a folder picker for selecting a single folder asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected storage folder, or null if the user cancelled the operation.</returns>
        Task<StorageFolder?> PickSingleFolder(CancellationToken cancellationToken = default);
    }
}
