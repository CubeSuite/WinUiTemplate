using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using WinUiTemplate.MVVM.Models;
using WinUiTemplate.MVVM.Models.ViewModels;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class LoggerService : ILoggerService
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IProgramData programData;
        private readonly IFileUtils fileUtils;

        // Members
        private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private readonly object consoleLock = new object();
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private StreamWriter? logWriter;
        private string currentLog = "";
        private bool paused = false;
        private bool initialised = false;
        private const int paddingSize = 6;

        private bool _logDebugToFile;
        
        // Properties
        
        public bool LogDebugToFile {
            get => _logDebugToFile || programData.IsDebugBuild;
            set => _logDebugToFile = value;
        }

        public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new ObservableCollection<LogEntryViewModel>();

        // Constructors

        public LoggerService(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            programData = serviceProvider.GetRequiredService<IProgramData>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
        }

        // Events

        public event Action OnFatal;

        // Public Functions

        public void LogDebug(string message, string[]? tags = null, bool shortenPaths = true) 
                 => LogMessage(LogLevel.Debug, message, shortenPaths, tags);

        public void LogInfo(string message, string[]? tags = null, bool shortenPaths = true) 
                 => LogMessage(LogLevel.Info, message, shortenPaths, tags);
        
        public void LogWarning(string message, string[]? tags = null, bool shortenPaths = true) 
                 => LogMessage(LogLevel.Warn, message, shortenPaths, tags);
        
        public void LogError(string message, string[]? tags = null, bool shortenPaths = true) 
                 => LogMessage(LogLevel.Error, message, shortenPaths, tags);
        
        public void LogFatal(string message, string[]? tags = null, bool shortenPaths = true) {
            LogMessage(LogLevel.Fatal, message, shortenPaths, tags);
            OnFatal?.Invoke();
        }
        
        public void Pause() {
            paused = true;

            try {
                logWriter?.Dispose();
                logWriter = null;
            }
            catch (Exception e){
                Debug.Assert(false, $"LoggerService.Pause failed: '{e.Message}'");
            }
        }

        public void Resume() {
            try {
                if (!string.IsNullOrWhiteSpace(currentLog)) {
                    logWriter = new StreamWriter(currentLog, append: true, Encoding.UTF8) { AutoFlush = true };
                }
            }
            catch (Exception e) {
                Debug.Assert(false, $"LoggerService.Resume failed: '{e.Message}'");
            }
        }

        // Private Functions

        private void Initialise() {
            if (!Directory.Exists(programData.FilePaths.LogsFolder)) {
                throw new DirectoryNotFoundException($"Logs folder missing: {programData.FilePaths.LogsFolder}");
            }

            string timestamp = fileUtils.GetFileSafeTimestamp();
            currentLog = Path.Combine(programData.FilePaths.LogsFolder, $"{timestamp}.log");

            logWriter = new StreamWriter(currentLog, append: false, Encoding.UTF8) { AutoFlush = true };
            _ = RotateLogsAsync();
            StartLoggingWorker();
            initialised = true;
        }

        private void StartLoggingWorker() {
            Task.Run(async () => {
                while (!tokenSource.IsCancellationRequested) {
                    await FlushQueueAsync();
                    await Task.Delay(50, tokenSource.Token);
                }

                await FlushQueueAsync();
            }, tokenSource.Token);
        }

        private async Task FlushQueueAsync() {
            if (paused || logWriter == null || queue.IsEmpty) return;

            while(queue.TryDequeue(out string? entry)) {
                await logWriter.WriteLineAsync(entry);
            }
        }

        private async Task RotateLogsAsync() {
            try {
                IUserSettings userSettings = serviceProvider.GetRequiredService<IUserSettings>();
                string folder = programData.FilePaths.LogsFolder;

                FilesResult result = await fileUtils.TryGetAllFilesAsync(folder);
                if (!result.Success || result.Files == null) throw new Exception(result.ErrorMessage);

                while(result.Files.Count > userSettings.MaxLogs) {
                    StorageFile? oldestLog = null;
                    DateTimeOffset oldestCreation = DateTime.MaxValue;

                    foreach(StorageFile file in result.Files) {
                        if (file.DateCreated < oldestCreation) {
                            oldestLog = file;
                            oldestCreation = file.DateCreated;
                        }
                    }

                    await oldestLog?.DeleteAsync();

                    result = await fileUtils.TryGetAllFilesAsync(folder);
                    if (!result.Success || result.Files == null) throw new Exception(result.ErrorMessage);
                }
            }
            catch (Exception e) {
                Debug.Assert(false, $"LoggerService.RotateLogsAsync failed: '{e.Message}'");
            }
        }

        private void LogMessage(LogLevel level, string message, bool shortenPaths, string[]? tags) {
            if (!initialised) Initialise();
            if (shortenPaths) message = message = message.Replace(programData.FilePaths.RootFolder, "");

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string thread = $"T{Environment.CurrentManagedThreadId}";
            string levelString = level.ToString().PadRight(paddingSize);
            string entry = $"{timestamp} | {thread} | {levelString}";
            if (tags != null && tags.Length != 0) {
                entry += $" | [{string.Join(", ", tags)}]";
            }

            entry += $" | {message}";

            lock (consoleLock) {
                Console.ForegroundColor = GetConsoleColour(level);
                Console.Write(levelString);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($" | {message}");
            }

            if (ShouldWriteToFile(level)) queue.Enqueue(entry);

            LogEntries.Add(new LogEntryViewModel(new LogEntry(
                level: level,
                message: message,
                tags: tags
            )));
        }

        private bool ShouldWriteToFile(LogLevel level) {
            return level != LogLevel.Debug || LogDebugToFile;
        }

        private ConsoleColor GetConsoleColour(LogLevel level) {
            return level switch {
                LogLevel.Info => ConsoleColor.Green,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }
}
