using System;

namespace WinUiTemplate.Stores.Interfaces
{
    public interface IProgramData
    {
        // Properties
        IFilePaths FilePaths { get; }
        bool IsDebugBuild { get; }
        string ProgramName { get; }
        Version ProgramVersion { get; }
        bool RunUnitTests { get; }
        bool EnableBackups { get; }
        EncryptionLevel EncryptionLevel { get; }
        bool UsesApi { get; }
    }
}