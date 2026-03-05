using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;

#pragma warning disable CS0067 // Event is never used

namespace WinUiTemplate.Services.Testing
{
    public class TestBackupService : IBackupService
    {
        public event Action<ZipProgress>? ProgressChanged;
        public event Action? BackupCreated;
        public event Action? BackupDeleted;

        public Task<OperationResult> CreateBackupAsync(CancellationToken cancellationToken) {
            return Task.FromResult(new OperationResult(false, "Intentional fail for test", true));
        }

        public Task<OperationResult> RestoreBackupAsync(string zipPath, CancellationToken cancellationToken) {
            return Task.FromResult(new OperationResult(false, "Intentional fail for test", true));
        }

        public Task<OperationResult> DeleteBackupAsync(string zipPath, CancellationToken cancellationToken) {
            return Task.FromResult(new OperationResult(false, "Intentional fail for test", true));
        }

        public Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken) {
            return Task.FromResult<IReadOnlyList<BackupInfo>>(new List<BackupInfo>());
        }

        public Task CheckMaxBackups() {
            return Task.CompletedTask;
        }
    }
}
