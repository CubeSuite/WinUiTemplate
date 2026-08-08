using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class ObjectCache<T, V> : IObjectCache<T, V> where T : notnull
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Fields
        private readonly Dictionary<T, V> cache = new Dictionary<T, V>();
        private readonly object cacheLock = new object();

        // Properties

        public IEnumerable<V> Values {
            get {
                lock (cacheLock) {
                    return cache.Values.ToList();
                }
            }
        }

        public IEnumerable<T> Keys {
            get {
                lock (cacheLock) {
                    return cache.Keys.ToList();
                }
            }
        }

        public int Count {
            get {
                lock (cacheLock) {
                    return cache.Count;
                }
            }
        }

        // Constructors

        public ObjectCache(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Public Functions

        public OperationResult TryAdd(T key, V instance) {
            lock (cacheLock) {
                if (cache.ContainsKey(key)) {
                    string errorMessage = $"Key '{key}' already exists in cache";
                    logger.LogWarning(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                cache.Add(key, instance);
                return new OperationResult(true, null, false);
            }
        }

        public OperationResult TryUpdate(T key, V instance) {
            lock (cacheLock) {
                if (!cache.ContainsKey(key)) {
                    string errorMessage = $"Key '{key}' does not exist in cache";
                    logger.LogWarning(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                cache[key] = instance;
                return new OperationResult(true, null, false);
            }
        }

        public OperationResult TryDelete(T key) {
            lock (cacheLock) {
                if (!cache.ContainsKey(key)) {
                    string errorMessage = $"Key '{key}' does not exist in cache";
                    logger.LogError(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                cache.Remove(key);
                return new OperationResult(true, null, false);
            }
        }

        public OperationResult TryGet(T key, out V? value, bool suppressErrors = false) {
            lock (cacheLock) {
                if (!cache.TryGetValue(key, out value)) {
                    string errorMessage = $"Key '{key}' does not exist in cache";
                    if (!suppressErrors) {
                        logger.LogError(errorMessage);
                    }
                    return new OperationResult(false, errorMessage, suppressErrors);
                }

                return new OperationResult(true, null, false);
            }
        }

        public bool ContainsKey(T key) {
            lock (cacheLock) {
                return cache.ContainsKey(key);
            }
        }

        public virtual OperationResult Clear() {
            lock (cacheLock) {
                cache.Clear();
                return new OperationResult(true, null, false);
            }
        }
    }
}
