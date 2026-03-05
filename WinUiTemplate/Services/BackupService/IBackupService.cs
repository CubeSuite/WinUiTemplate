using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    public interface IBackupService
    {
        // Events
        
        /// <summary>
        /// Event raised when backup operation progress changes.
        /// </summary>
        event Action<ZipProgress>? ProgressChanged;
        
        /// <summary>
        /// Event raised when a new backup has been created.
        /// </summary>
        event Action? BackupCreated;

        /// <summary>
        /// Event raised when a new backup has been created.
        /// </summary>
        event Action? BackupDeleted;

        // Public Functions

        /// <summary>
        /// Creates a new backup of the application data.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> indicating success or failure with error information.</returns>
        Task<OperationResult> CreateBackupAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Restores application data from a backup zip file.
        /// </summary>
        /// <param name="zipPath">The path to the backup zip file to restore.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> indicating success or failure with error information.</returns>
        Task<OperationResult> RestoreBackupAsync(string zipPath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a backup zip file.
        /// </summary>
        /// <param name="zipPath">The path to the backup zip file to delete.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> indicating success or failure with error information.</returns>
        Task<OperationResult> DeleteBackupAsync(string zipPath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves a list of all available backups.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A read-only list of <see cref="BackupInfo"/> objects representing available backups.</returns>
        Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);
    }
}
