using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for archive operations including compression and extraction.
    /// </summary>
    public interface IArchiveService
    {
        // Events
        /// <summary>
        /// Occurs when the progress of a zip operation changes.
        /// </summary>
        event Action<ZipProgress>? ProgressChanged;

        // Public Functions

        /// <summary>
        /// Compresses the contents of a folder into a zip archive asynchronously.
        /// </summary>
        /// <param name="sourceFolder">The path to the folder whose contents will be compressed.</param>
        /// <param name="zipFilePath">The path where the zip file will be created.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The operation result containing the success status and any error messages if the operation failed.</returns>
        Task<OperationResult> ZipFolderAsync(string sourceFolder, string zipFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts the contents of a zip archive to a destination folder asynchronously.
        /// </summary>
        /// <param name="zipPath">The path to the zip file to extract.</param>
        /// <param name="destinationFolder">The path to the folder where the contents will be extracted.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The operation result containing the success status and any error messages if the operation failed.</returns>
        Task<OperationResult> ExtractZip(string zipPath, string destinationFolder, CancellationToken cancellationToken = default);
    }
}
