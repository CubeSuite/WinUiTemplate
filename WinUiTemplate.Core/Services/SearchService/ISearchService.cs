using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Services.Interfaces
{
    public interface ISearchService
    {
        // Public Functions

        /// <summary>
        /// Filters a collection of items by matching each search token derived from <paramref name="query"/>
        /// against the values produced by the provided <paramref name="selectors"/>.
        /// Respects the <c>SearchCaseSensitive</c> and <c>SearchSplitQuery</c> user settings.
        /// </summary>
        /// <typeparam name="T">The type of the items to search.</typeparam>
        /// <param name="items">The collection of items to filter.</param>
        /// <param name="query">The search query string.</param>
        /// <param name="selectors">
        /// One or more functions that extract searchable values from an item.
        /// Each selector may return a scalar value or an <see cref="IEnumerable{T}"/> of values.
        /// </param>
        /// <returns>
        /// A task that resolves to the subset of <paramref name="items"/> that match every token in the query.
        /// </returns>
        Task<IEnumerable<T>> Search<T>(IEnumerable<T> items, string query, params Func<T, object?>[] selectors);

        /// <summary>
        /// Determines whether a single item would appear in search results for the given query.
        /// </summary>
        /// <typeparam name="T">The type of the item to test.</typeparam>
        /// <param name="item">The item to test.</param>
        /// <param name="query">The search query string.</param>
        /// <param name="selectors">
        /// One or more functions that extract searchable values from the item.
        /// Each selector may return a scalar value or an <see cref="IEnumerable{T}"/> of values.
        /// </param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the item matches every token in the query; otherwise <c>false</c>.
        /// </returns>
        Task<bool> AppearsInSearch<T>(T item, string query, params Func<T, object?>[] selectors);
    }
}
