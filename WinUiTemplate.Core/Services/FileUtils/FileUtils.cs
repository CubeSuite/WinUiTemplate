using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class FileUtils : IFileUtils
    {
        // Services & Stores
        private readonly IProgramData programData;
        private readonly IDialogService dialogService;
        private readonly IEncryptionService encryptionService;

        // Members
        private const string timestampFormat = "yyyy-MM-dd HH-mm-ss";
        private const string _encryptedFileHeader = "WinUiTemplateEF:";

        // Properties
        private string AppName => programData.ProgramName;
        public string EncryptedFileHeader => _encryptedFileHeader;

        // Constructors

        public FileUtils(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
        }

        // Public Functions

        public async Task<FileResult> TryGetFileAsync(string path) {
            StorageFile? file = await ResolveFileAsync(path);
            bool success = file != null;
            string error = success ? "" : "File not accessible or does not exist";
            return new FileResult(success, error, file);
        }

        public async Task<FileReadResult> TryReadFileAsync(string path) {
            StorageFile? file = await ResolveFileAsync(path);
            if (file == null) return new FileReadResult(false, "File not accessible", "");

            try {
                string content = await FileIO.ReadTextAsync(file);
                if (content.StartsWith(_encryptedFileHeader)) {
                    content = content.Substring(_encryptedFileHeader.Length);
                    content = await encryptionService.DecryptFromBase64Async(content);
                }

                return new FileReadResult(true, "", content);
            }
            catch (Exception e) {
                return new FileReadResult(false, e.Message, "");
            }
        }

        public async Task<FileWriteResult> TryWriteFileAsync(string path, string content, bool? encrypt = null) {
            if(encrypt == null) {
                encrypt = programData.EncryptionLevel == EncryptionLevel.Data &&
                          path.Contains(programData.FilePaths.DataFolder);
            }
            
            if (encrypt == true) content = _encryptedFileHeader + await encryptionService.EncryptToBase64Async(content);

            StorageFile? file = await ResolveOrCreateFileAsync(path);
            if (file == null) return new FileWriteResult(false, "File not accessible");

            try {
                await FileIO.WriteTextAsync(file, content);
                return new FileWriteResult(true, "");
            }
            catch (Exception e) {
                return new FileWriteResult(false, e.Message);
            }
        }

        public async Task<FileWriteResult> TryWriteFileAsync(string path, IEnumerable<string> lines, bool? encrypt = null) {
            return await TryWriteFileAsync(path, string.Join(Environment.NewLine, lines), encrypt);
        }

        public async Task<OperationResult> TryDeleteFileAsync(string path) {
            StorageFile? file = await ResolveFileAsync(path);
            if (file == null) return new OperationResult(false, "File not accessible", true);

            try {
                await file.DeleteAsync();
                return new OperationResult(true, "", false);
            }
            catch (Exception e) {
                return new OperationResult(false, e.Message, true);
            }
        }

        public async Task<FolderResult> TryGetOrCreateFolderAsync(string path) {
            StorageFolder? folder = await ResolveOrCreateFolderAsync(path);
            if (folder == null) return new FolderResult(false, "Folder not accessible or created", null);

            return new FolderResult(true, "", folder);
        }

        public async Task<FilesResult> TryGetAllFilesAsync(string path, CancellationToken cancellationToken = default) {
            List<StorageFile> files = new List<StorageFile>();

            StorageFolder? folder = await ResolveFolderAsync(path);
            if (folder == null) return new FilesResult(false, "Folder not accessible", files);

            IReadOnlyList<StorageFile> directFiles = await folder.GetFilesAsync();
            foreach(StorageFile file in directFiles) {
                cancellationToken.ThrowIfCancellationRequested();
                files.Add(file);
            }

            IReadOnlyList<StorageFolder> subFolders = await folder.GetFoldersAsync();
            foreach(StorageFolder subFolder in subFolders) {
                cancellationToken.ThrowIfCancellationRequested();

                FilesResult subFolderResult = await TryGetAllFilesAsync(subFolder.Path, cancellationToken);
                if (!subFolderResult.Success || subFolderResult.Files == null) {
                    return new FilesResult(false, "Failed to get files from subFolder", null);
                }

                files.AddRange(subFolderResult.Files);
            }

            return new FilesResult(true, "", files);
        }

        public async Task<OperationResult> TryDeleteFolderAsync(string path) {
            StorageFolder? folder = await ResolveFolderAsync(path);
            if (folder == null) return new OperationResult(false, "Folder not accessible", true);

            try {
                await folder.DeleteAsync();
                return new OperationResult(true, "", false);
            }
            catch (Exception e) {
                return new OperationResult(false, e.Message, true);
            }
        }

        public async Task<bool> EnsureFolderAccessAsync(string path) {
            StorageFolder? folder = await ResolveFolderAsync(path);
            return folder != null;
        }

        public async Task CreateProgramFolderStructure() {
            List<string> folders = [
                programData.FilePaths.RootFolder,
                programData.FilePaths.DataFolder,
                programData.FilePaths.LogsFolder,
                programData.FilePaths.CrashReportsFolder
            ];

            foreach (string folder in folders) {
                FolderResult result = await TryGetOrCreateFolderAsync(folder);
                if (!result.Success) {
                    await dialogService.ShowMessage(
                        MessageType.Error, "Failed To Launch", 
                        $"{AppName} failed to create it's necessary folder structure and will now close. " +
                        $"Please report this issue to the developer."
                    );

                    throw new Exception($"Failed to create necessary folder: '{folder}' - {result.ErrorMessage}");
                }
            }
        }

        public string GetRelativePath(string rootPath, string fullPath) {
            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) {
                return fullPath;
            }

            return fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public string GetFileSafeTimestamp() {
            return DateTime.Now.ToString(timestampFormat);
        }

        public DateTime ParseFileSafeTimestamp(string timestamp) {
            if (timestamp.Contains(".")) timestamp.Split('.').First();
            return DateTime.ParseExact(timestamp, timestampFormat, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        // Private Functions

        private async Task<StorageFolder?> ResolveFolderAsync(string path) {
            if (!Directory.Exists(path)) return null;

            foreach(AccessListEntry entry in StorageApplicationPermissions.FutureAccessList.Entries) {
                if (entry.Metadata != path) continue;

                try {
                    return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(entry.Token);
                }
                catch {
                    StorageApplicationPermissions.FutureAccessList.Remove(entry.Token);
                }
            }

            try {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                StorageApplicationPermissions.FutureAccessList.Add(folder, path);
                return folder;
            }
            catch {
                await dialogService.ShowMessage(
                    MessageType.Warning, "Couldn't Access Folder",
                    $"{programData.ProgramName} couldn't access the folder '{path}'. You need to select it in " +
                    $"the window that is about to open."
                );

                StorageFolder? picked = await dialogService.PickSingleFolder();
                if (picked == null) return null;
                if (picked.Path != null) return null;

                StorageApplicationPermissions.FutureAccessList.Add(picked, picked.Path);
                return picked;
            }
        }

        private async Task<StorageFolder?> ResolveOrCreateFolderAsync(string path) {
            StorageFolder? folder = await ResolveFolderAsync(path);
            if (folder != null) return folder;

            string? parent = Path.GetDirectoryName(path);
            string name = Path.GetFileName(path);

            StorageFolder? parentFolder = await ResolveFolderAsync(parent ?? "");
            if (parentFolder == null) return null;

            try {
                StorageFolder? created = await parentFolder.CreateFolderAsync(name);
                StorageApplicationPermissions.FutureAccessList.Add(created, path);
                return created;
            }
            catch {
                return null;
            }
        }

        private async Task<StorageFile?> ResolveFileAsync(string path) {
            try {
                return await StorageFile.GetFileFromPathAsync(path);
            }
            catch {
                return null;
            }
        }

        private async Task<StorageFile?> ResolveOrCreateFileAsync(string path) {
            StorageFile? file = await ResolveFileAsync(path);
            if (file != null) return file;

            string? folderPath = Path.GetDirectoryName(path);
            string name = Path.GetFileName(path);

            StorageFolder? folder = await ResolveOrCreateFolderAsync(folderPath ?? "");
            if (folder == null) return null;

            try {
                return await folder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
            }
            catch {
                return null;
            }
        }
    }
}
