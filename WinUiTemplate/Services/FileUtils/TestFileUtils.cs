using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services.Testing
{
    public class TestFileUtils : IFileUtils
    {
        // Properties
        public string EncryptedFileHeader { get; } = "";

        // Public Functions

        public Task InitializeAsync() {
            return Task.CompletedTask;
        }

        public Task<FileResult> TryGetFileAsync(string path) {
            FileResult result = new FileResult(false, "Test failure", null);
            return Task.FromResult(result);
        }

        public Task<FileReadResult> TryReadFileAsync(string path) {
            FileReadResult result = new FileReadResult(false, "Test failure", "");
            return Task.FromResult(result);
        }

        public Task<FileWriteResult> TryWriteFileAsync(string path, string content, bool? encrypt = null) {
            FileWriteResult result = new FileWriteResult(false, "Test failure");
            return Task.FromResult(result);
        }

        public Task<FileWriteResult> TryWriteFileAsync(string path, IEnumerable<string> lines, bool? encrypt = null) {
            FileWriteResult result = new FileWriteResult(false, "Test failure");
            return Task.FromResult(result);
        }

        public Task<OperationResult> TryDeleteFileAsync(string path) {
            OperationResult result = new OperationResult(false, "Test failure", true);
            return Task.FromResult(result);
        }

        public Task<OperationResult> TryCreateFolderAsync(string path) {
            OperationResult result = new OperationResult(false, "Test failure", true);
            return Task.FromResult(result);
        }

        public Task<OperationResult> TryDeleteFolderAsync(string path) {
            OperationResult result = new OperationResult(false, "Test failure", true);
            return Task.FromResult(result);
        }

        public Task<FolderResult> TryGetOrCreateFolderAsync(string path) {
            FolderResult result = new FolderResult(false, "Test failure", null);
            return Task.FromResult(result);
        }

        public Task<bool> EnsureFolderAccessAsync(string path) {
            return Task.FromResult(false);
        }

        public async Task CreateProgramFolderStructure() {
            
        }

        public string GetFileSafeTimestamp() {
            return "";
        }

        public DateTime ParseFileSafeTimestamp(string timestamp) {
            return new DateTime();
        }

        public Task<FilesResult> TryGetAllFilesAsync(string path, CancellationToken cancellationToken) {
            FilesResult result = new FilesResult(false, "Test failure", null);
            return Task.FromResult(result);
        }

        public string GetRelativePath(string rootPath, string fullPath) {
            return "";
        }
    }
}
