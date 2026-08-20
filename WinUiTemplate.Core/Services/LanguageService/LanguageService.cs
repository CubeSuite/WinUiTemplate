using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Services
{
    public class LanguageService : ILanguageService
    {
        // Fields
        private readonly string keyPrefix;

        // Constructors

        /// <summary>
        /// All string resources live in the single Strings\{lang}\Resources.resw file, keyed by
        /// "{keyPrefix}.{key}" (e.g. "MainWindow.BackupFailed"). Pass the owning page/window name
        /// as keyPrefix so callers can keep using short keys like "BackupFailed".
        /// </summary>
        public LanguageService(string keyPrefix) {
            this.keyPrefix = keyPrefix;
        }

        // Public Functions

        public static void ReloadLanguage() {
            ResourceContext.ResetGlobalQualifierValues();
        }

        public string Get(string key) => ResourceLoader.GetForViewIndependentUse().GetString($"{keyPrefix}/{key}");

        public CultureInfo GetCulture(LanguageOption language) {
            return new CultureInfo(language.ToLanguageCode());
        }
    }
}
