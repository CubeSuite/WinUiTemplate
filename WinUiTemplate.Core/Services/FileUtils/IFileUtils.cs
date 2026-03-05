using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUiTemplate.Services.Interfaces
{
    public interface IFileUtils
    {
        // Properties
        string EncryptedFileHeader { get; }

        // Public Functions

        /// <summary>
        /// Attempts to retrieve a file from the specified path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A <see cref="FileResult"/> containing the file if successful, or error information if failed.</returns>
        Task<FileResult> TryGetFileAsync(string path);
        
        /// <summary>
        /// Attempts to read the contents of a file from the specified path.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A <see cref="FileReadResult"/> containing the file content if successful, or error information if failed.</returns>
        Task<FileReadResult> TryReadFileAsync(string path);
        
        /// <summary>
        /// Attempts to write text content to a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="content">The string content to write to the file.</param>
        /// <returns>A <see cref="FileWriteResult"/> indicating success or failure with error information.</returns>
        Task<FileWriteResult> TryWriteFileAsync(string path, string content, bool? encrypt = null);
        
        /// <summary>
        /// Attempts to write multiple lines to a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="lines">The collection of lines to write to the file.</param>
        /// <returns>A <see cref="FileWriteResult"/> indicating success or failure with error information.</returns>
        Task<FileWriteResult> TryWriteFileAsync(string path, IEnumerable<string> lines, bool? encrypt = null);
        
        /// <summary>
        /// Attempts to delete a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file to delete.</param>
        /// <returns>An <see cref="OperationResult"/> indicating success or failure with error information.</returns>
        Task<OperationResult> TryDeleteFileAsync(string path);

        /// <summary>
        /// Attempts to retrieve or create a folder at the specified path.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        /// <returns>A <see cref="FolderResult"/> containing the folder if successful, or error information if failed.</returns>
        Task<FolderResult> TryGetOrCreateFolderAsync(string path);
        
        /// <summary>
        /// Attempts to retrieve all files from the specified folder path.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="FilesResult"/> containing the collection of files if successful, or error information if failed.</returns>
        Task<FilesResult> TryGetAllFilesAsync(string path, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Attempts to delete a folder at the specified path.
        /// </summary>
        /// <param name="path">The path to the folder to delete.</param>
        /// <returns>An <see cref="OperationResult"/> indicating success or failure with error information.</returns>
        Task<OperationResult> TryDeleteFolderAsync(string path);

        /// <summary>
        /// Ensures that the application has access to the specified folder path.
        /// </summary>
        /// <param name="path">The path to the folder to check access for.</param>
        /// <returns>A boolean indicating whether access to the folder was granted.</returns>
        Task<bool> EnsureFolderAccessAsync(string path);
        
        /// <summary>
        /// Creates the necessary folder structure for the program.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateProgramFolderStructure();

        /// <summary>
        /// Gets the relative path from a root path to a full path.
        /// </summary>
        /// <param name="rootPath">The root path to calculate from.</param>
        /// <param name="fullPath">The full path to calculate to.</param>
        /// <returns>A string representing the relative path.</returns>
        string GetRelativePath(string rootPath, string fullPath);
        
        /// <summary>
        /// Gets a file-safe timestamp string for the current date and time.
        /// </summary>
        /// <returns>A string representing a timestamp safe for use in file names.</returns>
        string GetFileSafeTimestamp();
        
        /// <summary>
        /// Parses a file-safe timestamp string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="timestamp">The file-safe timestamp string to parse.</param>
        /// <returns>A <see cref="DateTime"/> object representing the parsed timestamp.</returns>
        DateTime ParseFileSafeTimestamp(string timestamp);
    }
}
