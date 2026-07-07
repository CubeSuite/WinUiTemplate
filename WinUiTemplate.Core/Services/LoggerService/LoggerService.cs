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
using WinUiTemplate.Core.MVVM.Models;
using WinUiTemplate.Core.MVVM.Models.ViewModels;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Services
{
    public class LoggerService : ILoggerService, IDisposable
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IProgramData programData;
        private readonly IFileUtils fileUtils;

        // Fields
        private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private readonly object consoleLock = new object();
        private readonly object initialiseLock = new object();
        private readonly SemaphoreSlim writerLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private Task? _workerTask;
        private int _disposed = 0;
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

        public event Action? OnFatal;

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
            writerLock.Wait();
            try {
                paused = true;
                try {
                    logWriter?.Dispose();
                    logWriter = null;
                }
                catch (Exception e) {
                    Debug.Assert(false, $"LoggerService.Pause failed: '{e.Message}'");
                }
            }
            finally {
                writerLock.Release();
            }
        }

        public void Resume() {
            writerLock.Wait();
            try {
                try {
                    if (!string.IsNullOrWhiteSpace(currentLog)) {
                        logWriter = new StreamWriter(currentLog, append: true, Encoding.UTF8) { AutoFlush = true };
                    }
                    paused = false;
                }
                catch (Exception e) {
                    Debug.Assert(false, $"LoggerService.Resume failed: '{e.Message}'");
                }
            }
            finally {
                writerLock.Release();
            }
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            tokenSource.Cancel();
            _workerTask?.GetAwaiter().GetResult();
            logWriter?.Dispose();
            writerLock.Dispose();
            tokenSource.Dispose();
        }

        // Private Functions

        private void EnsureInitialised() {
            lock (initialiseLock) {
                if (!initialised) Initialise();
            }
        }

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
            _workerTask = Task.Run(async () => {
                try {
                    while (!tokenSource.IsCancellationRequested) {
                        await FlushQueueAsync();
                        try {
                            await Task.Delay(50, tokenSource.Token);
                        }
                        catch (OperationCanceledException) {
                            break;
                        }
                    }
                }
                finally {
                    await FlushQueueAsync();
                }
            });
        }

        private async Task FlushQueueAsync() {
            if (queue.IsEmpty) return;

            await writerLock.WaitAsync();
            try {
                if (paused || logWriter == null) return;

                while (queue.TryDequeue(out string? entry)) {
                    await logWriter.WriteLineAsync(entry);
                }
            }
            finally {
                writerLock.Release();
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
            EnsureInitialised();
            if (shortenPaths) message = message.Replace(programData.FilePaths.RootFolder, "");

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
