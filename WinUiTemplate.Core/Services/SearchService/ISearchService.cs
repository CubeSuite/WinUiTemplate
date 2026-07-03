using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Services
{
    public interface ISearchService
    {
        // Public Functions
        Task<IEnumerable<T>> Search<T>(IEnumerable<T> items, string query, params Func<T, object?>[] selectors);
        Task<bool> AppearsInSearch<T>(T item, string query, params Func<T, object?>[] selectors);
    }
}
