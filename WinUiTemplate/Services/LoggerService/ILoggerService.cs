using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for logging messages at various severity levels.
    /// </summary>
    public interface ILoggerService
    {
        // Properties
        /// <summary>
        /// A value indicating whether debug messages should be logged to a file.
        /// </summary>
        bool LogDebugToFile { get; set; }

        // Public Functions
        /// <summary> 
        /// Logs a debug-level message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tags">Optional tags to categorize or filter the log entry.</param>
        /// <param name="shortenPaths">If true, file paths in the message will have the root folder path stripped from them.</param>
        void LogDebug(string message, string[]? tags = null, bool shortenPaths = true);

        /// <summary>
        /// Logs an information-level message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tags">Optional tags to categorize or filter the log entry.</param>
        /// <param name="shortenPaths">If true, file paths in the message will have the root folder path stripped from them.</param>
        void LogInfo(string message, string[]? tags = null, bool shortenPaths = true);

        /// <summary>
        /// Logs a warning-level message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tags">Optional tags to categorize or filter the log entry.</param>
        /// <param name="shortenPaths">If true, file paths in the message will have the root folder path stripped from them.</param>
        void LogWarning(string message, string[]? tags = null, bool shortenPaths = true);

        /// <summary>
        /// Logs an error-level message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tags">Optional tags to categorize or filter the log entry.</param>
        /// <param name="shortenPaths">If true, file paths in the message will have the root folder path stripped from them.</param>
        void LogError(string message, string[]? tags = null, bool shortenPaths = true);

        /// <summary>
        /// Logs a fatal-level message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tags">Optional tags to categorize or filter the log entry.</param>
        /// <param name="shortenPaths">If true, file paths in the message will have the root folder path stripped from them.</param>
        void LogFatal(string message, string[]? tags = null, bool shortenPaths = true);

        /// <summary>
        /// Pauses logging temporarily.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes logging after it has been paused.
        /// </summary>
        void Resume();

        // Events

        /// <summary>
        /// Raised when a fatal-level message is logged.
        /// </summary>
        event Action OnFatal;
    }
}
