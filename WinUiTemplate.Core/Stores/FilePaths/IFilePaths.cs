namespace WinUiTemplate.Core.Stores.Interfaces
{
    public interface IFilePaths
    {
        // Properties
        string CrashReportsFolder { get; }
        string DataFolder { get; }
        string LogsFolder { get; }
        string RootFolder { get; }

        string TempMetadataFile { get; }
        string SettingsFile { get; }
        string KeyFile { get; }
        string Database { get; }
    }
}