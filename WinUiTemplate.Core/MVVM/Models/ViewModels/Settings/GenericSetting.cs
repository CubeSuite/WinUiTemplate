using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public class GenericSetting<T> : SettingBase
    {
        // Members
        private readonly Func<T> getValue;
        private readonly Action<T> setValue;

        // Properties

        public T Value {
            get => getValue();
            set => setValue(value);
        }

        // Constructors

        public GenericSetting(string name, string description, string icon,
                              Func<T> getValueFunc, Action<T> setValueFunc, string type = "")
                             :base(name, description, icon) 
        {
            getValue = getValueFunc;
            setValue = setValueFunc;

            Type = type == "" ? typeof(T).ToString() : type;
        }
    }
}
