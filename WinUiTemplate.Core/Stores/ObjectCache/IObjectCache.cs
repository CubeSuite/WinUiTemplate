using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services;

namespace WinUiTemplate.Core.Stores
{
    public interface IObjectCache<T, V>
    {
        // Public Functions
        OperationResult TryAdd(T key, V instance);
        OperationResult TryUpdate(T key, V instance);
        OperationResult TryGet(T key, out V value);
        OperationResult TryDelete(T key);
        bool ContainsKey(T key);
        OperationResult Clear();

        // Properties
        IEnumerable<V> Values { get; }
        IEnumerable<T> Keys { get; }
        int Count { get; }
    }
}
