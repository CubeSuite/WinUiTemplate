using Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class FilePaths : IFilePaths
    {
        // Properties
        public string RootFolder => ApplicationData.Current.LocalFolder.Path;
        public string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public string DataFolder => $"{RootFolder}\\Data";
        public string ImageCacheFolder => $"{CacheFolder}\\ImageCache";
        public string LogsFolder => $"{RootFolder}\\Logs";
        public string CrashReportsFolder => $"{RootFolder}\\CrashReports";

        public string TempMetadataFile => $"{RootFolder}\\metadata.json";
        public string SettingsFile => $"{DataFolder}\\Settings.json";
        public string ImageCacheSaveFile => $"{DataFolder}\\ImageCache.json";
        public string KeyFile => $"{DataFolder}\\EncryptionKey.bin";
        public string Database => $"{DataFolder}\\Database.db";
    }
}
