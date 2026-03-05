using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class BackupService : IBackupService
    {
        // Services & Stores
        private readonly IProgramData programData;
        private readonly IUserSettings userSettings;
        private readonly IFileUtils fileUtils;
        private readonly IDialogService dialogService;
        private readonly INotificationService notificationService;
        private readonly ILoggerService logger;
        private readonly IArchiveService archiveService;

        // Members
        private const int bufferSize = 81920;

        // Constructors
        public BackupService(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            archiveService = serviceProvider.GetRequiredService<IArchiveService>();

            archiveService.ProgressChanged += progress => ProgressChanged?.Invoke(progress);
        }

        // Events
        public event Action<ZipProgress>? ProgressChanged;
        public event Action? BackupCreated;
        public event Action? BackupDeleted;

        // Public Functions

        public async Task<OperationResult> CreateBackupAsync(CancellationToken cancellationToken = default) {
            if (!programData.EnableBackups) return new OperationResult(false, "Backups are disabled", false);
            if (string.IsNullOrWhiteSpace(userSettings.BackupsFolder)) return new OperationResult(false, "Backups folder not configured", false);

            OperationResult validateResult = await ValidateBackupFolderAsync();
            if (!validateResult.Success) return validateResult;

            string fileName = fileUtils.GetFileSafeTimestamp() + ".zip";
            string zipPath = Path.Combine(userSettings.BackupsFolder, fileName);

            await WriteBackupMetadata(zipPath, cancellationToken);
            OperationResult zipResult = await archiveService.ZipFolderAsync(programData.FilePaths.RootFolder, zipPath, cancellationToken);
            if (zipResult.Success) {
                await CheckMaxBackups();
                BackupCreated?.Invoke();
                notificationService.Notify(InfoBarSeverity.Success, "Successfully backed up data");
            }

            return zipResult;
        }

        public async Task<OperationResult> RestoreBackupAsync(string zipPath, CancellationToken cancellationToken = default) {
            if (!await dialogService.Confirm(
                "Restore Backup?",
                "Are you sure you want to restore this backup? A backup will be performed now in case you want to revert"
            )) {
                return new OperationResult(false, "User cancelled", false);
            }

            if (!(await CreateBackupAsync(cancellationToken)).Success) {
                return new OperationResult(false, "Failed to create backup", true);
            }
            
            if (string.IsNullOrWhiteSpace(zipPath)) return new OperationResult(false, $"Invalid backup path '{zipPath}'", true);

            FileResult zipResult = await fileUtils.TryGetFileAsync(zipPath);
            if (!zipResult.Success || zipResult.File == null) return new OperationResult(false, "Backup file not accessible", true);

            BackupInfo? info = await ReadBackupInfoAsync(zipResult.File, cancellationToken);
            if (info == null) return new OperationResult(false, "Failed to read backup metadata", true);

            if (info.CreatedWith > programData.ProgramVersion) return new OperationResult(false, "Backup was created with a newer version", true);
            if (info.CreatedWith < programData.ProgramVersion && 
                !await dialogService.Confirm(
                    "Use Outdated Backup?", 
                    $"This backup was created with an older version of {programData.ProgramName} " +
                    $"and may not be compatible. Are you sure you want to restore it?"
                )
            ) {
                return new OperationResult(false, "User cancelled due to outdated data", false);
            }

            FolderResult rootResult = await fileUtils.TryGetOrCreateFolderAsync(programData.FilePaths.RootFolder);
            if (!rootResult.Success || rootResult.Folder == null) return new OperationResult(false, "Root folder not accessible", true);

            logger.Pause();

            OperationResult result = await archiveService.ExtractZip(zipPath, programData.FilePaths.RootFolder, cancellationToken);
            if (!result.Success) return new OperationResult(false, result.ErrorMessage, true);

            FileResult metadataFileResult = await fileUtils.TryGetFileAsync(programData.FilePaths.TempMetadataFile);
            if (!metadataFileResult.Success || metadataFileResult.File == null) {
                return new OperationResult(true, "Couldn't access metadata.json", false);
            }

            try {
                await metadataFileResult.File.DeleteAsync();
            }
            catch (Exception e) {
                string error = $"Couldn't delete metadata.json: '{e.Message}'";
                Debug.Assert(false, error);
                return new OperationResult(true, error, false);
            }

            await dialogService.ShowMessage(
                MessageType.Success, "Restore Sucessful",
                $"{programData.ProgramName} will now restart."
            );

            AppInstance.Restart("");
            return new OperationResult(true, "", false);
        }

        public async Task<OperationResult> DeleteBackupAsync(string zipPath, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(zipPath)) return new OperationResult(false, "Invalid backup path", true);
            
            if(!await dialogService.Confirm("Delete Backup?", "This cannot be undone.")) {
                return new OperationResult(false, "User cancelled", false);
            }

            FileResult fileResult = await fileUtils.TryGetFileAsync(zipPath);
            if (!fileResult.Success || fileResult.File == null) return new OperationResult(false, "Zip file not accessable", true);

            try {
                await fileResult.File.DeleteAsync();
                BackupDeleted?.Invoke();
                return new OperationResult(true, "", false);
            }
            catch (Exception e) {
                logger.LogError($"Backup deletion failed - {e.Message}");
                notificationService.Notify(InfoBarSeverity.Warning, "Delete Failed", e.Message);
                return new OperationResult(false, e.Message, true);
            }
        }

        public async Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default) {
            List<BackupInfo> backups = new List<BackupInfo>();

            if (string.IsNullOrWhiteSpace(userSettings.BackupsFolder)) return backups;

            OperationResult ensureResult = await ValidateBackupFolderAsync();
            if (!ensureResult.Success) return backups;

            StorageFolder? backupsFolder = await GetBackupFolderAsync();
            if (backupsFolder == null) return backups;

            IReadOnlyList<StorageFile> files = await backupsFolder.GetFilesAsync();
            foreach(StorageFile file in files) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!file.Path.EndsWith(".zip")) continue;

                try {
                    BackupInfo? info = await ReadBackupInfoAsync(file, cancellationToken);
                    if (info == null) continue;
                    backups.Add(info);
                }
                catch {
                }
            }

            return backups;
        }

        // Private Functions

        private async Task CheckMaxBackups() {
            if (!await fileUtils.EnsureFolderAccessAsync(userSettings.BackupsFolder)) {
                logger.LogError("Could not check max backups, access not granted");
                return;
            }

            IReadOnlyList<BackupInfo> backups = await GetBackupsAsync();
            if (backups.Count == 0) return;

            while (backups.Count > userSettings.MaxBackups) {
                string oldestBackup = "";
                DateTime oldestTime = DateTime.MaxValue;

                foreach (BackupInfo info in backups) {
                    string zip = info.Path;
                    DateTime created = File.GetCreationTime(zip);
                    if (created < oldestTime) {
                        oldestTime = created;
                        oldestBackup = zip;
                    }
                }

                try {
                    File.Delete(oldestBackup);
                    logger.LogInfo($"Deleted old backup: {Path.GetFileName(oldestBackup)}");
                }
                catch (Exception ex) {
                    logger.LogError($"Failed to delete old backup '{oldestBackup}' - {ex.Message}");
                    return;
                }

                backups = await GetBackupsAsync();
            }
        }

        private async Task<OperationResult> ValidateBackupFolderAsync() {
            if (string.IsNullOrWhiteSpace(userSettings.BackupsFolder)) {
                return new OperationResult(false, "Backups folder is not configured", false);
            }

            FolderResult createResult = await fileUtils.TryGetOrCreateFolderAsync(userSettings.BackupsFolder);
            if (!createResult.Success) {
                return new OperationResult(false, $"Failed to get or create backups folder '{userSettings.BackupsFolder}'", false);
            }

            return new OperationResult(true, "", false);
        }

        private async Task<StorageFolder?> GetBackupFolderAsync() {
            FolderResult result = await fileUtils.TryGetOrCreateFolderAsync(userSettings.BackupsFolder);
            if (!result.Success) return null;
            return result.Folder;
        }

        private async Task<ulong> GetTotalSizeAsync(IEnumerable<StorageFile> files, CancellationToken cancellationToken) {
            ulong total = 0;
            foreach(StorageFile file in files) {
                cancellationToken.ThrowIfCancellationRequested();
                total += (await file.GetBasicPropertiesAsync()).Size;
            }

            return total;
        }

        private async Task<OperationResult> WriteBackupMetadata(string zipPath, CancellationToken cancellationToken) {
            FilesResult filesResult = await fileUtils.TryGetAllFilesAsync(programData.FilePaths.RootFolder, cancellationToken);
            if (!filesResult.Success || filesResult.Files == null) return new OperationResult(false, "Failed to get data files", true);

            ulong totalBytes = await GetTotalSizeAsync(filesResult.Files, cancellationToken);
            Version createdWith = programData.ProgramVersion;
            DateTime created = DateTime.Now;

            BackupInfo metadata = new BackupInfo(zipPath, created, createdWith, (long)totalBytes);
            string json = JsonConvert.SerializeObject(metadata);
            FileWriteResult result = await fileUtils.TryWriteFileAsync(programData.FilePaths.TempMetadataFile, json, false);
            return new OperationResult(result.Success, result.ErrorMessage, !result.Success);
        }

        private async Task<BackupInfo?> ReadBackupInfoAsync(StorageFile zipFile, CancellationToken cancellationToken) {
            DateTime created = DateTime.UtcNow;
            Version createdWith = new Version(1, 0, 0, 0);
            long size = 0;

            BasicProperties props = await zipFile.GetBasicPropertiesAsync();
            created = props.DateModified.UtcDateTime;
            size = (long)props.Size;

            try {
                using (Stream zipStream = await zipFile.OpenStreamForReadAsync()) {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, false, Encoding.UTF8)) {
                        ZipArchiveEntry? jsonFile = null;
                        foreach (ZipArchiveEntry entry in archive.Entries) {
                            if (string.Equals(entry.FullName, "metadata.json", StringComparison.OrdinalIgnoreCase)) {
                                jsonFile = entry;
                                break;
                            }
                        }

                        if(jsonFile == null) {
                            string error = $"Failed to read backup metadata for '{zipFile.Name}'";
                            logger.LogError(error);
                            notificationService.Notify(InfoBarSeverity.Error, "Backup Error", error);
                            return null;
                        }

                        using (Stream metaStream = jsonFile.Open()) {
                            using (MemoryStream ms = new MemoryStream()) {
                                await metaStream.CopyToAsync(ms, bufferSize, cancellationToken);
                                string json = Encoding.UTF8.GetString(ms.ToArray());
                                return JsonConvert.DeserializeObject<BackupInfo>(json);
                            }
                        }
                    }
                }
            }
            catch {
                string error = $"Failed to read backup metadata for '{zipFile.Name}'";
                logger.LogError(error);
                notificationService.Notify(InfoBarSeverity.Error, error);
                return null;
            }
        }
    }
}
