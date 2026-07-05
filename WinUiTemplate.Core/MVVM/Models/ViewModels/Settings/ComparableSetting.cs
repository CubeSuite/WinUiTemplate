using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels.Settings
{
    public class ComparableSetting<T> : SettingBase where T : IComparable
    {
        // Fields
        private ILoggerService logger;

        private Func<T> getValue;
        private Action<T> setValue;

        private static readonly Type[] typesToCheck = [typeof(int), typeof(float), typeof(double)];

        // Properties

        public T Min { get; }
        public T Max { get; }

        public T Value {
            get => getValue();
            set {
                if (typesToCheck.Contains(typeof(T))) {
                    if (Min != null && value.CompareTo(Min) < 0) {
                        logger.LogWarning($"Tried to set setting '{Name}' to '{value}' which is less than min '{Min}'");
                        value = Min;
                    }

                    if (Max != null && value.CompareTo(Max) > 0) {
                        logger.LogWarning($"Tried to set setting '{Name}' to '{value}' which is greater than max '{Max}'");
                        value = Max;
                    }
                }

                setValue(value);
            }
        }

        // Constructors

        public ComparableSetting(string name, string description, string icon,
                                 Func<T> getValueFunc, Action<T> setValueFunc, T min, T max, IServiceProvider serviceProvider, Func<bool>? isVisibleFunc = null)
                                :base(name, description, icon, "Comparable") 
        {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            getValue = getValueFunc;
            setValue = setValueFunc;
            getIsVisibleFunc = isVisibleFunc;

            Min = min;
            Max = max;
        }
    }
}
