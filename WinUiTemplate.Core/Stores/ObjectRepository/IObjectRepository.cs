using WinUiTemplate.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.Stores.ObjectCache
{
    public interface IObjectRepository<T, V> : IObjectCache<T, V>
    {
        // Properties
        string TableName { get; }

        // Public Functions

        /// <summary>
        /// Executes a custom SQL query and returns results as instances of V
        /// </summary>
        IEnumerable<V> Query(string sql, params object[] parameters);

        /// <summary>
        /// Executes a custom SQL command (INSERT, UPDATE, DELETE) and returns the number of affected rows
        /// </summary>
        OperationResult ExecuteCommand(string sql, params object[] parameters);
    }
}
