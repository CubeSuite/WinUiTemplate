# Unit Testing Guide for ArchiveService

## Quick Decision Framework: "Should I Write Unit Tests?"

### ✅ YES - Write Unit Tests When:
- **Public API**: Method is public and used by other code
- **Complex Logic**: Multiple code paths, calculations, or conditions
- **Error Handling**: Multiple failure scenarios to verify
- **Business Critical**: Bugs would impact users significantly
- **Has Dependencies**: Uses interfaces that can be mocked
- **Regression Prevention**: Bug-prone areas that broke before

### ❌ NO - Skip Unit Tests When:
- **Simple Pass-Through**: Method just calls another method with no logic
- **UI Code**: Direct UI manipulation (use integration/UI tests instead)
- **Third-Party Wrappers**: Just wrapping external libraries
- **Framework Code**: Testing the framework itself, not your logic
- **Configuration**: Simple getters/setters with no logic

## ArchiveService Analysis

### `ZipFolderAsync` - ✅ SHOULD TEST
**Why:**
- Complex async logic with multiple failure points
- Error handling for file access, cancellation, exceptions
- Progress reporting functionality
- Dependencies on `IFileUtils` that can be mocked
- Business critical - backup functionality

**Test Scenarios:**
1. ✅ Success: Successfully creates zip from folder
2. ✅ Error: Source folder doesn't exist
3. ✅ Error: Cannot create destination zip file
4. ✅ Cancellation: Respects CancellationToken
5. ✅ Progress: ProgressChanged event fires correctly
6. ✅ Edge: Empty folder
7. ✅ Edge: Large files with progress updates
8. ✅ Exception: Unexpected errors are logged and returned

### `ExtractZip` - ✅ SHOULD TEST
**Why:**
- Complex extraction logic with error handling
- Multiple failure scenarios (missing zip, bad destination)
- Cancellation support
- File system operations that can be mocked

**Test Scenarios:**
1. ✅ Success: Successfully extracts zip to folder
2. ✅ Error: Zip file doesn't exist
3. ✅ Error: Cannot create destination folder
4. ✅ Cancellation: Respects CancellationToken
5. ✅ Edge: Nested folder structures
6. ✅ Exception: Corrupt zip file handling

## Implementation Guide

### Step 1: Create Test Project

```bash
# For .NET 8 with Windows support
dotnet new xunit -n WinUiTemplate.Tests -f net8.0-windows10.0.19041.0
cd WinUiTemplate.Tests

# Add required packages
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0

# Add reference to main project
dotnet add reference ../WinUiTemplate/WinUiTemplate.csproj
```

### Step 2: Update Test Project File

Edit `WinUiTemplate.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Platforms>x64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WinUiTemplate\WinUiTemplate.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: Create Test Class

Create `Services/ArchiveServiceTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using Windows.Storage;
using Xunit;

namespace WinUiTemplate.Tests.Services
{
    public class ArchiveServiceTests
    {
        private readonly Mock<IFileUtils> _mockFileUtils;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ArchiveService _archiveService;

        public ArchiveServiceTests()
        {
            // Create mocks
            _mockFileUtils = new Mock<IFileUtils>();
            _mockLogger = new Mock<ILoggerService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // Setup service provider to return mocked services
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IFileUtils)))
                .Returns(_mockFileUtils.Object);
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(_mockLogger.Object);

            // Create service under test
            _archiveService = new ArchiveService(_mockServiceProvider.Object);
        }

        #region ZipFolderAsync Tests

        [Fact]
        public async Task ZipFolderAsync_SourceFolderDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var sourceFolder = @"C:\NonExistent";
            var zipPath = @"C:\output.zip";

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null));
            
            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(sourceFolder))
                .ReturnsAsync(new FolderResult(false, null));

            // Act
            var result = await _archiveService.ZipFolderAsync(sourceFolder, zipPath);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("sourceFolder");
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task ZipFolderAsync_CannotCreateZipFile_ReturnsFailure()
        {
            // Arrange
            var sourceFolder = @"C:\Source";
            var zipPath = @"C:\Invalid\Path\output.zip";
            var mockFolder = new Mock<StorageFolder>();

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(sourceFolder))
                .ReturnsAsync(new FolderResult(true, mockFolder.Object));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(@"C:\Invalid\Path"))
                .ReturnsAsync(new FolderResult(false, null));

            // Act
            var result = await _archiveService.ZipFolderAsync(sourceFolder, zipPath);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("zip parent directory");
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task ZipFolderAsync_CancellationRequested_ReturnsCancelled()
        {
            // Arrange
            var sourceFolder = @"C:\Source";
            var zipPath = @"C:\output.zip";
            var cts = new CancellationTokenSource();
            var mockFolder = new Mock<StorageFolder>();
            var mockZipFolder = new Mock<StorageFolder>();
            var mockZipFile = new Mock<StorageFile>();

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(sourceFolder))
                .ReturnsAsync(new FolderResult(true, mockFolder.Object));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(@"C:\"))
                .ReturnsAsync(new FolderResult(true, mockZipFolder.Object));

            mockZipFolder
                .Setup(x => x.CreateFileAsync("output.zip", It.IsAny<CreationCollisionOption>()))
                .ReturnsAsync(mockZipFile.Object);

            // Cancel immediately
            cts.Cancel();

            // Act
            var result = await _archiveService.ZipFolderAsync(
                sourceFolder, 
                zipPath, 
                cts.Token
            );

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("cancelled");
        }

        [Fact]
        public async Task ZipFolderAsync_ProgressChanged_FiresCorrectly()
        {
            // Arrange
            var sourceFolder = @"C:\Source";
            var zipPath = @"C:\output.zip";
            var progressUpdates = new List<ZipProgress>();
            
            _archiveService.ProgressChanged += progress => progressUpdates.Add(progress);

            // Setup mocks for successful operation
            // ... (mock setup similar to above)

            // Act
            await _archiveService.ZipFolderAsync(sourceFolder, zipPath);

            // Assert
            progressUpdates.Should().NotBeEmpty();
            progressUpdates.Should().OnlyContain(p => p.Percent >= 0 && p.Percent <= 1);
            progressUpdates.Last().Percent.Should().BeApproximately(1.0, 0.01);
        }

        #endregion

        #region ExtractZip Tests

        [Fact]
        public async Task ExtractZip_ZipFileDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var zipPath = @"C:\nonexistent.zip";
            var destination = @"C:\Output";

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(false, null));

            // Act
            var result = await _archiveService.ExtractZip(zipPath, destination);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("zip file");
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task ExtractZip_CannotCreateDestination_ReturnsFailure()
        {
            // Arrange
            var zipPath = @"C:\archive.zip";
            var destination = @"C:\Invalid\Destination";
            var mockZipFile = new Mock<StorageFile>();

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(true, mockZipFile.Object));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(destination))
                .ReturnsAsync(new FolderResult(false, null));

            // Act
            var result = await _archiveService.ExtractZip(zipPath, destination);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("destination folder");
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task ExtractZip_CancellationRequested_ReturnsCancelled()
        {
            // Arrange
            var zipPath = @"C:\archive.zip";
            var destination = @"C:\Output";
            var cts = new CancellationTokenSource();
            var mockZipFile = new Mock<StorageFile>();
            var mockDestFolder = new Mock<StorageFolder>();

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(true, mockZipFile.Object));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(destination))
                .ReturnsAsync(new FolderResult(true, mockDestFolder.Object));

            cts.Cancel();

            // Act
            var result = await _archiveService.ExtractZip(zipPath, destination, cts.Token);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("cancelled");
        }

        [Fact]
        public async Task ExtractZip_UnexpectedException_LogsAndReturnsError()
        {
            // Arrange
            var zipPath = @"C:\archive.zip";
            var destination = @"C:\Output";
            var mockZipFile = new Mock<StorageFile>();
            var mockDestFolder = new Mock<StorageFolder>();

            _mockFileUtils
                .Setup(x => x.TryGetFileAsync(zipPath))
                .ReturnsAsync(new FileResult(true, mockZipFile.Object));

            _mockFileUtils
                .Setup(x => x.TryGetOrCreateFolderAsync(destination))
                .ReturnsAsync(new FolderResult(true, mockDestFolder.Object));

            // Simulate exception when opening stream
            mockZipFile
                .Setup(x => x.OpenStreamForReadAsync())
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act
            var result = await _archiveService.ExtractZip(zipPath, destination);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Unzip failed");
            result.IsError.Should().BeTrue();
            
            _mockLogger.Verify(
                x => x.LogError(It.IsAny<string>()), 
                Times.Once
            );
        }

        #endregion

        #region Integration-Style Tests (Optional)

        [Fact]
        public async Task ZipAndExtract_RoundTrip_PreservesFiles()
        {
            // This would be an integration test that actually creates
            // temp files and tests the full zip/extract cycle
            // Only include if you want integration tests
        }

        #endregion
    }
}
```

### Step 4: Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~ZipFolderAsync_SourceFolderDoesNotExist"

# Generate code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Key Testing Concepts

### 1. **Mocking**
Replace dependencies (IFileUtils, ILoggerService) with test doubles that you control.

```csharp
// Setup mock to return specific value
_mockFileUtils
    .Setup(x => x.TryGetFileAsync(It.IsAny<string>()))
    .ReturnsAsync(new FileResult(false, null));

// Verify mock was called
_mockFileUtils.Verify(
    x => x.TryGetFileAsync("expected.zip"), 
    Times.Once
);
```

### 2. **Arrange-Act-Assert Pattern**
```csharp
// Arrange: Setup test data and mocks
var input = "test";

// Act: Execute the method under test
var result = await service.Method(input);

// Assert: Verify the outcome
result.Should().BeTrue();
```

### 3. **Fluent Assertions**
More readable assertions:
```csharp
// Instead of: Assert.True(result.Success)
result.Success.Should().BeTrue();

// Instead of: Assert.Contains("error", result.Error)
result.Error.Should().Contain("error");

// Collection assertions
list.Should().HaveCount(3);
list.Should().Contain(x => x.Id == 5);
```

## Testing Anti-Patterns to Avoid

❌ **Testing Implementation Details**
```csharp
// BAD: Testing internal method calls
_mockFileUtils.Verify(x => x.InternalMethod(), Times.Once);
```

❌ **Over-Mocking**
```csharp
// BAD: Mocking concrete classes or value types
var mock = new Mock<string>(); // Can't mock sealed classes
```

❌ **Not Testing Edge Cases**
```csharp
// BAD: Only testing happy path
[Fact]
public async Task OnlyTestsSuccess() { ... }
```

✅ **Test Behavior, Not Implementation**
```csharp
// GOOD: Test observable behavior
result.Success.Should().BeTrue();
result.Error.Should().BeEmpty();
```

## Code Coverage Goals

- **Aim for 70-80%** coverage for business logic
- **100% coverage ≠ Good tests** (can have worthless tests)
- Focus on:
  - All public methods
  - All error paths
  - All edge cases
  - Critical business logic

## When to Write Integration Tests Instead

Use integration tests for:
- Actual file system operations
- Database interactions
- External API calls
- End-to-end workflows
- UI interactions

For `ArchiveService`, you might want BOTH:
- **Unit tests** for logic and error handling (using mocks)
- **Integration tests** for actual zip/extract operations with real files

## Summary

For your `ArchiveService`:

| Method | Should Test? | Priority | Test Count |
|--------|-------------|----------|------------|
| `ZipFolderAsync` | ✅ Yes | High | 8-10 tests |
| `ExtractZip` | ✅ Yes | High | 6-8 tests |

**Total recommended tests: 14-18 unit tests**

These will cover:
- Success scenarios
- Error conditions
- Cancellation
- Progress reporting
- Edge cases
- Exception handling

Start with the critical paths (success + major errors), then add edge cases as time permits.
