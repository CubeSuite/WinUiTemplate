using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Stores.Interfaces
{
    public enum EncryptionLevel {
        Settings, // Only EncryptedSettings
        Data // Everything in data folder
    }
}
