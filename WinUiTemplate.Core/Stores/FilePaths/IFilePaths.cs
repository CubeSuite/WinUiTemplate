namespace WinUiTemplate.Core.Stores.Interfaces
{
    public interface IFilePaths
    {
        // Properties
        string RootFolder { get; }
        string DataFolder { get; }
        string ImageCacheFolder { get; }
        string LogsFolder { get; }
        string CrashReportsFolder { get; }

        string TempMetadataFile { get; }
        string SettingsFile { get; }
        string ImageCacheSaveFile { get; }
        string KeyFile { get; }
        string Database { get; }
    }
}