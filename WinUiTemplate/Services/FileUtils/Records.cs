using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUiTemplate.Services
{
    public record FileResult(bool Success, string? ErrorMessage, StorageFile? File);
    public record FilesResult(bool Success, string? ErrorMessage, IReadOnlyList<StorageFile>? Files);
    public record FileReadResult(bool Success, string? ErrorMessage, string? Content);
    public record FileWriteResult(bool Success, string? ErrorMessage);
    public record FolderResult(bool Success, string? ErrorMessage, StorageFolder? Folder);
    public record OperationResult(bool Success, string? ErrorMessage, bool Notify);
}
