using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public partial class SettingBase : ObservableObject
    {
        // Properties

        public string Name { get; }
        public string Description { get; }
        public string Icon { get; }
        public string Type { get; set; }

        // Constructors

        public SettingBase(string name, string description, string icon) {
            Name = name;
            Description = description;
            Icon = icon;
            Type = "";

            if (!Description.EndsWith(".")) Description += ".";
        }

        public SettingBase(string name, string description, string icon, string type) {
            Name = name;
            Description = description;
            Icon = icon;

            if (!Description.EndsWith(".")) Description += ".";
            Type = type;
        }
    }
}
