using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class ObjectCache<T, V> : IObjectCache<T, V>
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Members
        private Dictionary<T, V> cache = new Dictionary<T, V>();

        // Properties

        public IEnumerable<V> Values => cache.Values;
        public IEnumerable<T> Keys => cache.Keys;
        public int Count => cache.Count;

        // Constructors

        public ObjectCache(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Public Functions

        public OperationResult TryAdd(T key, V instance) {
            if (cache.ContainsKey(key)) {
                string errorMessage = $"Key '{key}' already exists in cache";
                logger.LogWarning(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }

            cache.Add(key, instance);
            return new OperationResult(true, null, false);
        }

        public OperationResult TryUpdate(T key, V instance) {
            if (!cache.ContainsKey(key)) {
                string errorMessage = $"Key '{key}' does not exist in cache";
                logger.LogWarning(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }

            cache[key] = instance;
            return new OperationResult(true, null, false);
        }

        public OperationResult TryDelete(T key) {
            if (!cache.ContainsKey(key)) {
                string errorMessage = $"Key '{key}' does not exist in cache";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }

            cache.Remove(key);
            return new OperationResult(true, null, false);
        }

        public OperationResult TryGet(T key, out V value) {
            if (!cache.TryGetValue(key, out value)){
                string errorMessage = $"Key '{key}' does not exist in cache";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }

            return new OperationResult(true, null, false);
        }

        public bool ContainsKey(T key) {
            return cache.ContainsKey(key);
        }

        public OperationResult Clear() {
            cache.Clear();
            return new OperationResult(true, null, false);
        }
    }
}
