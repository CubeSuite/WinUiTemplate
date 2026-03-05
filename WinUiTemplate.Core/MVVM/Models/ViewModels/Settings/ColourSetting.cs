using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public partial class ColourSetting : GenericSetting<string>
    {
        // Constructors
        public ColourSetting(string name, string description, string icon,
                             Func<string> getValueFunc, Action<string> setValueFunc)
                            : base(name, description, icon, getValueFunc, setValueFunc, "Colour"){}

        // Commands

        [RelayCommand]
        private void PickNewColour() {

        }
    }
}
