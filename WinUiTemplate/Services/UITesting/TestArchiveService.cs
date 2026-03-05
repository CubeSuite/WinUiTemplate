using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services.Testing
{
    public class TestArchiveService : IArchiveService
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Constructors

        public TestArchiveService(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Events
        public event Action<ZipProgress>? ProgressChanged;

        // Public Functions
        public Task<OperationResult> ZipFolderAsync(string sourceFolder, string zipFilePath, CancellationToken cancellationToken = default) {
            logger.LogWarning($"TestArchiveService: ZipFolderAsync called with sourceFolder='{sourceFolder}', zipFilePath='{zipFilePath}'. Intentionally failing.");
            return Task.FromResult(new OperationResult(false, "TestArchiveService: Intentional failure in ZipFolderAsync", true));
        }

        public Task<OperationResult> ExtractZip(string zipPath, string destinationFolder, CancellationToken cancellationToken = default) {
            logger.LogWarning($"TestArchiveService: ExtractZip called with zipPath='{zipPath}', destinationFolder='{destinationFolder}'. Intentionally failing.");
            return Task.FromResult(new OperationResult(false, "TestArchiveService: Intentional failure in ExtractZip", true));
        }
    }
}
