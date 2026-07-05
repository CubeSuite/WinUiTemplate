using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Services;

namespace WinUiTemplate.Core.Stores
{
    /// <summary>
    /// A generic key/value cache store with guarded add, update, retrieve, and delete operations.
    /// </summary>
    /// <typeparam name="T">The type of the cache keys.</typeparam>
    /// <typeparam name="V">The type of the cached values.</typeparam>
    public interface IObjectCache<T, V>
    {
        // Properties

        /// <summary>Gets all values currently held in the cache.</summary>
        IEnumerable<V> Values { get; }

        /// <summary>Gets all keys currently held in the cache.</summary>
        IEnumerable<T> Keys { get; }

        /// <summary>Gets the number of entries currently held in the cache.</summary>
        int Count { get; }

        // Public Functions

        /// <summary>
        /// Adds a new entry to the cache.
        /// Fails if an entry with the same <paramref name="key"/> already exists.
        /// </summary>
        /// <param name="key">The key to associate with the cached instance.</param>
        /// <param name="instance">The value to store in the cache.</param>
        /// <returns>
        /// An <see cref="OperationResult"/> indicating success, or failure with an error message
        /// if the key is already present.
        /// </returns>
        OperationResult TryAdd(T key, V instance);

        /// <summary>
        /// Updates an existing cache entry.
        /// Fails if no entry with the given <paramref name="key"/> exists.
        /// </summary>
        /// <param name="key">The key of the entry to update.</param>
        /// <param name="instance">The new value to store for the key.</param>
        /// <returns>
        /// An <see cref="OperationResult"/> indicating success, or failure with an error message
        /// if the key is not found.
        /// </returns>
        OperationResult TryUpdate(T key, V instance);

        /// <summary>
        /// Retrieves a cached value by its key.
        /// Fails if no entry with the given <paramref name="key"/> exists.
        /// </summary>
        /// <param name="key">The key of the entry to retrieve.</param>
        /// <param name="value">
        /// When this method returns, contains the cached value associated with <paramref name="key"/>,
        /// or the default value of <typeparamref name="V"/> if the key was not found.
        /// </param>
        /// <returns>
        /// An <see cref="OperationResult"/> indicating success, or failure with an error message
        /// if the key is not found.
        /// </returns>
        OperationResult TryGet(T key, out V value);

        /// <summary>
        /// Gets all values currently held in the cache.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetAll() => Keys;

        /// <summary>
        /// Removes an entry from the cache.
        /// Fails if no entry with the given <paramref name="key"/> exists.
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>
        /// An <see cref="OperationResult"/> indicating success, or failure with an error message
        /// if the key is not found.
        /// </returns>
        OperationResult TryDelete(T key);

        /// <summary>
        /// Determines whether the cache contains an entry with the specified key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns><c>true</c> if the key exists in the cache; otherwise <c>false</c>.</returns>
        bool ContainsKey(T key);

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        /// <returns>An <see cref="OperationResult"/> indicating that the operation succeeded.</returns>
        OperationResult Clear();
    }
}
