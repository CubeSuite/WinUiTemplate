using WinUiTemplate.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Services
{
    public class SearchService : ISearchService
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Constructors

        public SearchService(IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
        }

        // Public Functions

        public async Task<IEnumerable<T>> Search<T>(IEnumerable<T> items, string query, params Func<T, object?>[] selectors) {
            IEnumerable<string> tokens = GetSearchTokens(query);

            return await Task.Run(() => {
                return items.Where(item => {
                    foreach (string token in tokens) {
                        bool tokenFound = false;

                        foreach (Func<T, object?> selector in selectors) {
                            if (tokenFound) break;

                            object? value = selector(item);
                            if (value == null) continue;

                            if (value is IEnumerable<object> collection) {
                                foreach (object element in collection) {
                                    if (element == null) continue;

                                    string asString = element?.ToString() ?? "";
                                    if (!userSettings.SearchCaseSensitive) asString = asString.ToLower();

                                    if (asString.Contains(token)) {
                                        tokenFound = true;
                                        break;
                                    }
                                }
                            }
                            else {
                                string asString = value?.ToString() ?? "";
                                if (!userSettings.SearchCaseSensitive) asString = asString.ToLower();

                                if (asString.Contains(token)) tokenFound = true;
                            }
                        }

                        if (!tokenFound) return false;
                    }

                    return true;
                });
            });
        }

        public async Task<bool> AppearsInSearch<T>(T item, string query, params Func<T, object?>[] selectors) {
            return (await Search<T>([item], query, selectors)).Count() == 1;
        }

        // Private Functions

        private IEnumerable<string> GetSearchTokens(string query) {
            string[] tokens = userSettings.SearchSplitQuery ? query.Split(' ') : [query];
            return userSettings.SearchCaseSensitive ? tokens : tokens.Select(token => token.ToLower());
        }
    }
}
