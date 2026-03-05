using Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Stores
{
    public class FilePaths : IFilePaths
    {
        // Properties
        public string RootFolder => ApplicationData.Current.LocalFolder.Path;
        public string DataFolder => $"{RootFolder}\\Data";
        public string LogsFolder => $"{RootFolder}\\Logs";
        public string CrashReportsFolder => $"{RootFolder}\\CrashReports";

        public string TempMetadataFile => $"{RootFolder}\\metadata.json";
        public string SettingsFile => $"{DataFolder}\\Settings.json";
        public string KeyFile => $"{DataFolder}\\EncryptionKey.bin";
    }
}
