using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WinUiTemplate.Core.Stores.EnumExtensions;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public class EnumSetting<T> : SettingBase where T : struct, Enum
    {
        // Fields
        private Dictionary<string, T> valueToDescriptionMap = GetValuesWithDescriptions<T>();
        private readonly Func<T> getValue;
        private readonly Action<T> setValue;

        // Properties
        public string[] Names { get; }
        public string SelectedOption {
            get => getValue().GetDescription();
            set {
                if (valueToDescriptionMap.TryGetValue(value, out T newValue)) {
                    setValue(newValue);
                }
                else {
                    Debug.Assert(false, $"valueToDescriptionMap doesn't contain key '{value}'");
                }
            }
        }

        // Constructors

        public EnumSetting(string name, string description, string icon, 
                           Func<T> getValueFunc, Action<T> setValueFunc) 
                          :base(name, description, icon, "Enum"
        ) {
            Names = valueToDescriptionMap.Keys.ToArray();
            getValue = getValueFunc;
            setValue = setValueFunc;
        }
    }
}
