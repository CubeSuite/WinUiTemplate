using System;

namespace WinUiTemplate.Core.Stores.Interfaces
{
    public interface IProgramData
    {
        // Properties
        IFilePaths FilePaths { get; }
        bool IsDebugBuild { get; }
        string ProgramName { get; }
        string ProgramNameNoSpaces { get; }
        Version ProgramVersion { get; }
        bool EnableBackups { get; }
        bool EnableSingleInstance { get; }
        EncryptionLevel EncryptionLevel { get; }
        bool UsesApi { get; }
        bool UsesRemoteDatabase { get; }
    }
}