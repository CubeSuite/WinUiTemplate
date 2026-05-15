using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services;

namespace WinUiTemplate.Core.Stores.ObjectCache
{
    public interface IObjectRepository<T, V> : IObjectCache<T, V>
    {
        // Additional repository-specific methods for database operations
        
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
