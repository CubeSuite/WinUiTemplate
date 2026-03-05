using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.MVVM.Models.ViewModels.Settings
{
    public class EnumSetting<T> : GenericSetting<T> where T : Enum
    {
        // Properties
        public T[] Options => (T[])Enum.GetValues(typeof(T));

        // Constructors

        public EnumSetting(string name, string description, string icon, 
                           Func<T> getValueFunc, Action<T> setValueFunc) 
                          :base(name, description, icon, getValueFunc, setValueFunc, "Enum"){}
    }
}
