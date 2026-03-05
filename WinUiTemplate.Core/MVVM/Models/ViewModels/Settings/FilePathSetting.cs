using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public partial class FilePathSetting : GenericSetting<string>
    {
        // Services & Stores
        private readonly IDialogService dialogService;

        // Members
        private PickerType pickerType;
        private string filter;

        // Constructors
        public FilePathSetting(string name, string description, string icon,
                               Func<string> getValueFunc, Action<string> setValueFunc,
                               IServiceProvider serviceProvider, PickerType type, string filter = "*")
                              : base(name, description, icon, getValueFunc, setValueFunc, "FilePath") {
            pickerType = type;
            this.filter = filter;

            dialogService = serviceProvider.GetRequiredService<IDialogService>();
        }

        // Commands

        [RelayCommand]
        private void ExecuteButtonAction() {
            switch (pickerType) {
                case PickerType.File: PickFile(); break;
                case PickerType.Folder: PickFolder(); break;
                default:
                    Debug.Assert(false, $"Unknown picker type: '{pickerType}'");
                    break;
            }
        }

        public enum PickerType
        {
            File,
            Folder
        }

        // Private Functions

        private async void PickFile() {
            StorageFile? file = await dialogService.PickSingleFile();
            if (file != null) {
                Value = file.Path;
                OnPropertyChanged(nameof(Value));
            }
        }

        private async void PickFolder() {
            StorageFolder? folder = await dialogService.PickSingleFolder();
            if (folder != null) {
                Value = folder.Path;
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}
