using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public class EncryptedSetting : SettingBase 
    {
        // Services & Stores
        IEncryptionService encryptionService;

        // Members
        private readonly Func<string> getValue;
        private readonly Action<string> setValue;
        private string _decrypted = "";

        // Properties

        public string Value {
            get => _decrypted;
            set {
                _decrypted = value;
                SetEncryptedValueAsync(value);
            }
        }

        // Constructors

        public EncryptedSetting(string name, string description, string icon,
                                Func<string> getValueFunc, Action<string> setValueFunc,
                                IServiceProvider serviceProvider)
                               :base(name, description, icon, "System.String") 
        {
            IProgramData programData = serviceProvider.GetRequiredService<IProgramData>();
            if (programData.EncryptionLevel == EncryptionLevel.None) {
                Debug.Assert(false, $"You need to set ProgramData.EncryptionLevel to at least Settings");
            }

            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
            getValue = getValueFunc;
            setValue = setValueFunc;

            GetDecryptedValueAsync();
        }

        // Private Functions

        private async void GetDecryptedValueAsync() {
            _decrypted = await encryptionService.DecryptFromBase64Async(getValue());
            OnPropertyChanged(nameof(Value));
        }

        private async void SetEncryptedValueAsync(string newValue) {
            setValue(await encryptionService.EncryptToBase64Async(newValue));
        }
    }
}
