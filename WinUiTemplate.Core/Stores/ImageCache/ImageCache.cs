using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class ImageCache : IImageCache
    {
        // Services & Stores
        private readonly INotificationService notificationService;
        private readonly IEncryptionService encryptionService;
        private readonly IDialogService dialogService;
        private readonly IUserSettings userSettings;
        private readonly IProgramData programData;
        private readonly ILoggerService logger;
        private readonly IFileUtils fileUtils;

        // Fields
        private static readonly HttpClient httpClient = new HttpClient();
        private ObjectCache<string, string> imageCache;
        private bool loaded = false;

        // Properties
        private string SaveFile => programData.FilePaths.ImageCacheSaveFile;
        private long cacheSize => imageCache.Values.Where(file => File.Exists(file)).Sum(file => new FileInfo(file).Length);
        
        public string CacheSize => cacheSize switch {
            < 1_024 => $"{cacheSize} B",
            < 1_048_576 => $"{cacheSize / 1_024.0:F2} KB",
            < 1_073_741_824 => $"{cacheSize / 1_048_576.0:F2} MB",
            _ => $"{cacheSize / 1_073_741_824.0:F2} GB"
        };

        // Constructors

        public ImageCache(IServiceProvider serviceProvider) {
            notificationService = serviceProvider.GetRequiredService<INotificationService>();
            encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            programData = serviceProvider.GetRequiredService<IProgramData>();
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            fileUtils = serviceProvider.GetRequiredService<IFileUtils>();

            imageCache = new ObjectCache<string, string>(serviceProvider);
        }

        // Enums
        enum PathType
        {
            Relative,
            Local,
            Network,
            Internet,
            Unknown
        }

        // Public Functions

        public async Task<BitmapImage?> GetImage(string originalPath) {
            await WaitForLoad();

            string? cachedPath = "";
            if (imageCache.ContainsKey(originalPath) && 
                imageCache.TryGet(originalPath, out cachedPath) && 
                File.Exists(cachedPath)
            ) {
                return await LoadCachedImage(cachedPath);
            }

            switch (GetPathType(originalPath)) {
                case PathType.Local:
                case PathType.Network:
                    if (!File.Exists(originalPath)) {
                        string error = $"Local/Network can't be cached as it does not exist: '{originalPath}'";
                        logger.LogError(error);
                        Debug.Assert(false, error);
                        return null;
                    }

                    cachedPath = imageCache.TryGet(originalPath, out cachedPath, true) ? cachedPath : originalPath;
                    break;

                case PathType.Internet:
                    if (await CacheInternetImage(originalPath)) {
                        imageCache.TryGet(originalPath, out cachedPath, true);
                        break;
                    }

                    else return null;

                case PathType.Relative:
                case PathType.Unknown:
                default:
                    string unknownPathError = $"Cannot cache image with unknown or relative path type: '{originalPath}'";
                    logger.LogError(unknownPathError);
                    Debug.Assert(false, unknownPathError);
                    return null;
            }

            if (string.IsNullOrEmpty(cachedPath)) {
                string error = $"Cached path for: '{originalPath}' is null or empty";
                logger.LogError(error);
                Debug.Assert(false, error);
                return null;
            }

            if (!File.Exists(cachedPath)) { 
                string error = $"Cached file for: '{originalPath}' does not exist at path: '{cachedPath}'";
                logger.LogError(error);
                Debug.Assert(false, error);
                return null;
            }

            return await LoadCachedImage(cachedPath);
        }
       
        public async Task Load() {
            try {
                FileReadResult result = await fileUtils.TryReadFileAsync(SaveFile);
                if (!result.Success || result.Content == null) {
                    if (File.Exists(SaveFile)) {
                        string error = $"Failed to load image cache save file: '{SaveFile}'";
                        logger.LogError(error);
                        Debug.Assert(false, error);
                    }
                    loaded = true;
                    return;
                }

                Dictionary<string, string>? savedCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Content);
                if (savedCache == null) {
                    string error = $"Failed to de-serialise image cache save file: '{SaveFile}'";
                    logger.LogError(error);
                    Debug.Assert(false, error);
                    loaded = true;
                    return;
                }

                foreach (KeyValuePair<string, string> pair in savedCache) {
                    if (!imageCache.TryAdd(pair.Key, pair.Value)) {
                        string error = $"Failed to add image cache entry during load: '{pair.Key}'";
                        logger.LogError(error);
                        Debug.Assert(false, error);
                    }
                }

                if (cacheSize > (long)userSettings.ImageCacheWarnSizeGb * 1_073_741_824) {
                    notificationService.Notify(
                        InfoBarSeverity.Warning, $"Image Cache > {userSettings.ImageCacheWarnSizeGb} GB", 
                        "The image cache has grown larger than your chosen warning limit. Either clear it or expand the limit.",
                        buttonText: "Clear Cache", onClick: async () => await ClearCache()
                    );
                }

                loaded = true;
                logger.LogInfo("Finished loading image cache.");
            }
            catch (Exception e) {
                Debug.Assert(false, $"ImageCache.Load failed: {e.Message}");
                loaded = true;
            }
        }

        public async Task<OperationResult> ClearCache() {
            if (!await dialogService.Confirm("Clear Image Cache?", "Are you sure you want do delete all cached images?")) {
                return new OperationResult(false, "Declined", false);
            }

            FilesResult result = await fileUtils.TryGetAllFilesAsync(programData.FilePaths.ImageCacheFolder);
            if (!result.Success || result.Files == null) {
                string error = $"Failed to clear image cache: '{result.ErrorMessage}'";
                logger.LogError(error);
                Debug.Assert(false, error);
                return new OperationResult(false, result.ErrorMessage, true);
            }

            foreach(StorageFile file in result.Files) {
                try {
                    await file.DeleteAsync();
                }
                catch (Exception e) {
                    string error = $"Failed to delete cached image file: '{file.Path}' - {e.Message}";
                    logger.LogWarning(error);
                    Debug.Assert(false, error);
                }
            }

            imageCache.Clear();
            await Save();
            return new OperationResult(true, null, true);
        }

        // Private Functions

        private async Task WaitForLoad(int timeout_ms = 30000) {
            while (!loaded && timeout_ms > 0) {
                await Task.Delay(10);
                timeout_ms -= 10;
            }
        }

        private async Task Save() {
            try {
                Dictionary<string, string> cacheToSave = imageCache.Keys.Zip(imageCache.Values).ToDictionary(
                    pair => pair.First, pair => pair.Second
                );

                string json = JsonConvert.SerializeObject(cacheToSave);
                FileWriteResult result = await fileUtils.TryWriteFileAsync(SaveFile, json);
                if (result.Success) {
                    logger.LogInfo("Saved ImageCache");
                }
                else {
                    string error = $"Failed to save image cache data: '{SaveFile}'";
                    logger.LogError(error);
                    Debug.Assert(false, error);
                }
            }
            catch (Exception e) {
                string error = $"Failed to save image cache data: '{e.Message}'";
                logger.LogError(error);
                Debug.Assert(false, error);
            }
        }

        private PathType GetPathType(string path) {
            if (!Uri.TryCreate(path, UriKind.Absolute, out Uri? uri)) {
                return PathType.Relative;
            }

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) {
                return PathType.Internet;
            }

            if (uri.Scheme == Uri.UriSchemeFile) {
                return PathType.Local;
            }

            if (uri.Scheme == Uri.UriSchemeFtp || uri.Scheme == Uri.UriSchemeGopher || uri.Scheme == Uri.UriSchemeNews || uri.Scheme == Uri.UriSchemeNntp || uri.Scheme == Uri.UriSchemeTelnet) {
                return PathType.Network;
            }

            return PathType.Unknown;
        }

        private async Task<bool> CacheLocalOrNetworkImage(string path) {
            if (!File.Exists(path)) {
                string error = $"Failed to cache local/network image - file does not exist: '{path}'";
                logger.LogError(error);
                Debug.Assert(false, error);
                return false;
            }

            byte[] bytes = await File.ReadAllBytesAsync(path);
            string newName = Guid.NewGuid().ToString() + Path.GetExtension(path);
            string newPath = Path.Combine(programData.FilePaths.ImageCacheFolder, newName);

            if (!await SaveBytes(bytes, newPath)) return false;

            if (userSettings.ImageCacheEnabled) {
                OperationResult result = imageCache.TryAdd(path, newPath);
                if (!result) {
                    string error = $"Failed to add image cache entry for local/network image: '{path}' - {result.ErrorMessage}";
                    logger.LogError(error);
                    Debug.Assert(false, error);
                    return false;
                }
            }

            await Save();
            return true;
        }

        private async Task<bool> CacheInternetImage(string url) {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible)");

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                string error = $"Failed to download image from: '{url}' - {response.ReasonPhrase}";
                logger.LogError(error);
                Debug.Assert(false, error);
                return false;
            }

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            string ext = contentType switch {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".bin"
            };

            string fileName = Guid.NewGuid().ToString() + ext;
            string newPath = Path.Combine(programData.FilePaths.ImageCacheFolder, fileName);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();

            if (!await SaveBytes(bytes, newPath)) return false;

            if (userSettings.ImageCacheEnabled) {
                OperationResult result = imageCache.TryAdd(url, newPath);
                if (!result) {
                    string error = $"Failed to add image cache entry for internet image: '{url}' - {result.ErrorMessage}";
                    logger.LogError(error);
                    Debug.Assert(false, error);
                    return false;
                }
            }

            await Save();
            return true;
        }

        private async Task<bool> SaveBytes(byte[] bytes, string path) {
            if (!userSettings.ImageCacheEnabled) return true;

            if (programData.EncryptionLevel == EncryptionLevel.Data) {
                bytes = await encryptionService.EncryptAsync(bytes);
            }

            try {
                await File.WriteAllBytesAsync(path, bytes);
                return true;
            }
            catch (Exception e) {
                string error = $"Failed to save image to cache folder: '{path}', Error: {e.Message}";
                logger.LogError(error);
                Debug.Assert(false, error);
                return false;
            }
        }

        private async Task<BitmapImage> LoadCachedImage(string path) {
            byte[] bytes = await File.ReadAllBytesAsync(path);
            if (programData.EncryptionLevel == EncryptionLevel.Data) {
                bytes = await encryptionService.DecryptAsync(bytes);
            }

            BitmapImage image = new BitmapImage();
            using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            using DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0));
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await image.SetSourceAsync(stream);
            return image;
        }
    }
}
