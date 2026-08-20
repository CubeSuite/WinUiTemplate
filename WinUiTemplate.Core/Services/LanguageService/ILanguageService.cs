using System.Globalization;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Services.Interfaces
{
    public interface ILanguageService
    {
        string Get(string key);
        CultureInfo GetCulture(LanguageOption language);
    }
}