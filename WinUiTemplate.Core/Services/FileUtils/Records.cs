using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUiTemplate.Core.Services.Interfaces
{
    public record FileResult(bool Success, string? ErrorMessage, StorageFile? File)
    {
        public static implicit operator bool(FileResult result) => result.Success;
    }

    public record FilesResult(bool Success, string? ErrorMessage, IReadOnlyList<StorageFile>? Files)
    {
        public static implicit operator bool(FilesResult result) => result.Success;
    }

    public record FileReadResult(bool Success, string? ErrorMessage, string? Content)
    {
        public static implicit operator bool(FileReadResult result) => result.Success;
    }

    public record FileWriteResult(bool Success, string? ErrorMessage)
    {
        public static implicit operator bool(FileWriteResult result) => result.Success;
    }

    public record FolderResult(bool Success, string? ErrorMessage, StorageFolder? Folder)
    {
        public static implicit operator bool(FolderResult result) => result.Success;
    }

    public record OperationResult(bool Success, string? ErrorMessage, bool Notify)
    {
        public static implicit operator bool(OperationResult result) => result.Success;
    }
}
