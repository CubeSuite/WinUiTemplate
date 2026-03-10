using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Stores
{
    public class ProgramData : IProgramData
    {
        // Properties
        public bool IsDebugBuild {
            get {
                #if DEBUG
                    return true;
                #else
                    return false;
                #endif
            }
        }
        public string ProgramName => "WinUiTemplate"; // ToDo: Set Program Name
        public string ProgramNameNoSpaces => ProgramName.Replace(" ", "");
        public Version ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        public IFilePaths FilePaths { get; }
        public bool EnableBackups { get; } = true; // ToDo: Set EnableBackups
        public EncryptionLevel EncryptionLevel { get; } = EncryptionLevel.None; // ToDo: Set EncryptionLevel
        public bool UsesApi { get; } = false; // ToDo: Set UsesApi

        // Constructors 

        public ProgramData() {
            FilePaths = new FilePaths();
        }
    }
}
