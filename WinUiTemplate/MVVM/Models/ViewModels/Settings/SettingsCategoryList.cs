using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public partial class SettingsCategoryList : ObservableObject
    {
        // Properties
        public string Category { get; }
        public IReadOnlyList<SettingBase> Settings { get; }

        // Constructors
        public SettingsCategoryList(string category, IEnumerable<SettingBase> settings) {
            Category = category;
            Settings = settings.ToList();
        }
    }
}
