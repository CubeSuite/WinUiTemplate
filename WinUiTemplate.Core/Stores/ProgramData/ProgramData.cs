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
        public bool RunUnitTests => false;
        public string ProgramName => "WinUiTemplate";
        public string ProgramNameNoSpaces => ProgramName.Replace(" ", "");
        public Version ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        public IFilePaths FilePaths { get; }
        public bool EnableBackups { get; } = true;
        public bool EncryptData { get; } = false;
        public EncryptionLevel EncryptionLevel { get; } = EncryptionLevel.None;
        public bool UsesApi { get; } = false;

        // Constructors 

        public ProgramData() {
            FilePaths = new FilePaths();
        }
    }
}
