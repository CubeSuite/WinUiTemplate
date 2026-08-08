using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels.Settings
{
    public class EncryptedSetting : SettingBase 
    {
        // Services & Stores
        IEncryptionService encryptionService;

        // Fields
        private readonly Func<string> getValue;
        private readonly Action<string> setValue;
        private string _decrypted = "";

        // Properties

        public string Value {
            get => _decrypted;
            set {
                _decrypted = value;
                _ = SetEncryptedValueAsync(value);
            }
        }

        // Constructors

        public EncryptedSetting(string name, string description, string icon,
                                Func<string> getValueFunc, Action<string> setValueFunc,
                                IServiceProvider serviceProvider, Func<bool>? isVisibleFunc = null)
                               :base(name, description, icon, "System.String") 
        {
            IProgramData programData = serviceProvider.GetRequiredService<IProgramData>();
            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
            getValue = getValueFunc;
            setValue = setValueFunc;
            getIsVisibleFunc = isVisibleFunc;

            _ = GetDecryptedValueAsync();
        }

        // Private Functions

        private async Task GetDecryptedValueAsync() {
            _decrypted = await encryptionService.DecryptFromBase64Async(getValue());
            OnPropertyChanged(nameof(Value));
        }

        private async Task SetEncryptedValueAsync(string newValue) {
            setValue(await encryptionService.EncryptToBase64Async(newValue));
        }

        // Public Functions

        public override void NotifyValueChanged() {
            _ = GetDecryptedValueAsync();
        }
    }
}
