using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;

namespace WinUiTemplate.Core.Stores.Interfaces
{
    public interface IImageCache
    {
        // Properties
        public string CacheSize { get; }

        // Public Functions
        Task<BitmapImage?> GetImage(string originalPath);
        Task Load();
        Task<OperationResult> ClearCache();
    }
}