using FluentAssertions;
using Moq;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Tests
{
    public class ArchiveServiceTests
    {
        private readonly Mock<IFileUtils> _mockFileUtils;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ArchiveService _archiveService;

        public ArchiveServiceTests() {
            _mockFileUtils = new Mock<IFileUtils>();
            _mockLogger = new Mock<ILoggerService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(_mockFileUtils.Object);
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(_mockLogger.Object);

            _archiveService = new ArchiveService(_mockServiceProvider.Object);
        }

        [Fact]
        public async Task Test_ZipFolderAsync_SourceFolderDoesNotExist() {
            string sourceFolder = @"C:\NonExistent";
            string zipPath = @"C:\output.zip";

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null, null));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(sourceFolder))
                .ReturnsAsync(new FolderResult(false, null, null));

            OperationResult result = await _archiveService.ZipFolderAsync(sourceFolder, zipPath);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to access sourceFolder");
        }
    }
}