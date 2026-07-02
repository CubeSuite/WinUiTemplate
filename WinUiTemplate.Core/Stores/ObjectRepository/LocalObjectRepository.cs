using CommunityToolkit.WinUI.Helpers;
using WinUiTemplate.Core.Stores.ObjectCache;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores;
using WinUiTemplate.Stores.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace WinUiTemplate.Core.Stores
{
    public class LocalObjectRepository<T, V> : IObjectRepository<T, V>
    {
        // Services & Stores
        private readonly ILoggerService logger;

        // Fields
        private readonly string databasePath;
        private readonly FieldInfo[] fields;
        
        protected readonly string _tableName;

        // Properties

        public IEnumerable<V> Values {
            get {
                List<V> values = new List<V>();
                try {
                    using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                    connection.Open();

                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT * FROM {_tableName}";

                    using SqliteDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        V value = CreateInstanceFromReader(reader);
                        if (value != null) {
                            values.Add(value);
                        }
                    }
                }
                catch (Exception ex) {
                    logger.LogError($"Error retrieving values: {ex.Message}");
                }
                return values;
            }
        }

        public IEnumerable<T> Keys {
            get {
                List<T> keys = new List<T>();
                try {
                    using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                    connection.Open();

                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT Key FROM {_tableName}";

                    using SqliteDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        string keyString = reader.GetString(0);
                        T key = ConvertStringToKey(keyString);
                        keys.Add(key);
                    }
                }
                catch (Exception ex) {
                    logger.LogError($"Error retrieving keys: {ex.Message}");
                }
                return keys;
            }
        }

        public int Count {
            get {
                try {
                    using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                    connection.Open();

                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT COUNT(*) FROM {_tableName}";

                    object result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
                catch (Exception ex) {
                    logger.LogError($"Error retrieving count: {ex.Message}");
                    return 0;
                }
            }
        }

        public string TableName => _tableName;

        // Constructors

        public LocalObjectRepository(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            IProgramData programData = serviceProvider.GetRequiredService<IProgramData>();

            databasePath = programData.FilePaths.Database;
            _tableName = $"{typeof(V).Name}s";
            fields = typeof(V).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .ToArray();

            ValidatePropertyTypes();
            EnsureDatabaseExists();
            EnsureTableExists();
        }

        // Public Functions

        public OperationResult TryAdd(T key, V instance) {
            try {
                string keyString = ConvertKeyToString(key);

                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();

                StringBuilder columns = new StringBuilder("Key");
                StringBuilder values = new StringBuilder("@key");

                foreach (FieldInfo field in fields) {
                    columns.Append($", {field.Name}");
                    values.Append($", @{field.Name}");
                }

                command.CommandText = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";
                command.Parameters.AddWithValue("@key", keyString);

                foreach (FieldInfo field in fields) {
                    object? value = field.GetValue(instance);
                    Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                    if (targetType == typeof(Color) && value is Color colorValue) {
                        command.Parameters.AddWithValue($"@{field.Name}", colorValue.ToHex());
                    }
                    else if (IsCollectionType(targetType) && value != null) {
                        string jsonValue = JsonConvert.SerializeObject(value);
                        command.Parameters.AddWithValue($"@{field.Name}", jsonValue);
                    }
                    else {
                        command.Parameters.AddWithValue($"@{field.Name}", value ?? DBNull.Value);
                    }
                }

                command.ExecuteNonQuery();

                return new OperationResult(true, null, false);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) {
                string errorMessage = $"Key '{key}' already exists in repository";
                logger.LogWarning(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error adding item to repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryUpdate(T key, V instance) {
            try {
                string keyString = ConvertKeyToString(key);

                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();

                StringBuilder setClause = new StringBuilder();
                for (int i = 0; i < fields.Length; i++) {
                    if (i > 0) setClause.Append(", ");
                    setClause.Append($"{fields[i].Name} = @{fields[i].Name}");
                }

                command.CommandText = $"UPDATE {_tableName} SET {setClause} WHERE Key = @key";
                command.Parameters.AddWithValue("@key", keyString);

                foreach (FieldInfo field in fields) {
                    object value = field.GetValue(instance);
                    Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                    if (targetType == typeof(Color) && value is Color colorValue) {
                        command.Parameters.AddWithValue($"@{field.Name}", colorValue.ToHex());
                    }
                    else if (IsCollectionType(targetType) && value != null) {
                        string jsonValue = JsonConvert.SerializeObject(value);
                        command.Parameters.AddWithValue($"@{field.Name}", jsonValue);
                    }
                    else {
                        command.Parameters.AddWithValue($"@{field.Name}", value ?? DBNull.Value);
                    }
                }

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0) {
                    string errorMessage = $"Key '{key}' does not exist in repository";
                    logger.LogWarning(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                return new OperationResult(true, null, false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error updating item in repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryDelete(T key) {
            try {
                string keyString = ConvertKeyToString(key);

                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {_tableName} WHERE Key = @key";
                command.Parameters.AddWithValue("@key", keyString);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0) {
                    string errorMessage = $"Key '{key}' does not exist in repository";
                    logger.LogError(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                return new OperationResult(true, null, false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error deleting item from repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryGet(T key, out V value) {
            value = default(V);

            try {
                string keyString = ConvertKeyToString(key);

                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {_tableName} WHERE Key = @key";
                command.Parameters.AddWithValue("@key", keyString);

                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read()) {
                    value = CreateInstanceFromReader(reader);
                    return new OperationResult(true, null, false);
                }

                string errorMessage = $"Key '{key}' does not exist in repository";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error retrieving item from repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryGetRange(T[] keys, out V[] values) {
            List<V> results = new List<V>();

            if (keys.Length == 0) {
                values = Array.Empty<V>();
                return new OperationResult(true, null, false);
            }

            try {
                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();

                string[] parameterNames = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++) {
                    parameterNames[i] = $"@p{i}";
                    command.Parameters.AddWithValue($"@p{i}", ConvertKeyToString(keys[i]));
                }

                command.CommandText = $"SELECT * FROM {_tableName} WHERE Key IN ({string.Join(", ", parameterNames)})";

                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read()) {
                    V value = CreateInstanceFromReader(reader);
                    if (value != null) {
                        results.Add(value);
                    }
                }

                values = results.ToArray();
                return new OperationResult(values.Length == keys.Length, null, false);
            }
            catch (Exception ex) {
                logger.LogError($"Error retrieving range: {ex.Message}");
                values = Array.Empty<V>();
                return new OperationResult(false, ex.Message, false);
            }
        }

        public bool ContainsKey(T key) {
            try {
                string keyString = ConvertKeyToString(key);

                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {_tableName} WHERE Key = @key";
                command.Parameters.AddWithValue("@key", keyString);

                object result = command.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex) {
                logger.LogError($"Error checking if key exists: {ex.Message}");
                return false;
            }
        }

        public virtual OperationResult Clear() {
            try {
                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {_tableName}";
                command.ExecuteNonQuery();

                return new OperationResult(true, null, false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error clearing repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public IEnumerable<V> GetAll() => Values;

        // Private Functions

        private void ValidatePropertyTypes() {
            List<string> unsupportedFields = new List<string>();

            foreach (FieldInfo field in fields) {
                Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                // Check if type is supported for serialization
                bool isSupported =
                    targetType == typeof(int) ||
                    targetType == typeof(long) ||
                    targetType == typeof(short) ||
                    targetType == typeof(byte) ||
                    targetType == typeof(bool) ||
                    targetType == typeof(float) ||
                    targetType == typeof(double) ||
                    targetType == typeof(decimal) ||
                    targetType == typeof(byte[]) ||
                    targetType == typeof(DateTime) ||
                    targetType == typeof(Guid) ||
                    targetType == typeof(Color) ||
                    targetType == typeof(string) ||
                    targetType.IsEnum ||
                    IsCollectionType(targetType);

                if (!isSupported) {
                    unsupportedFields.Add($"{field.Name} ({targetType.Name})");
                }
            }

            if (unsupportedFields.Count > 0) {
                string errorMessage = $"Type '{typeof(V).Name}' contains unsupported field types: {string.Join(", ", unsupportedFields)}. " +
                    "Supported types: int, long, short, byte, bool, float, double, decimal, byte[], DateTime, Guid, Color, string, enums, and collections (List, Array, HashSet, Dictionary).";
                logger.LogError(errorMessage);
                throw new NotSupportedException(errorMessage);
            }
        }

        private bool IsCollectionType(Type type) {
            // Check for common collection types that can be JSON serialized
            if (type.IsArray) return true;
            if (type.IsGenericType) {
                Type genericDef = type.GetGenericTypeDefinition();
                return genericDef == typeof(List<>) ||
                       genericDef == typeof(HashSet<>) ||
                       genericDef == typeof(Dictionary<,>) ||
                       genericDef == typeof(IList<>) ||
                       genericDef == typeof(ICollection<>) ||
                       genericDef == typeof(IEnumerable<>) ||
                       genericDef == typeof(IDictionary<,>);
            }
            return false;
        }

        private void EnsureDatabaseExists() {
            try {
                string directory = Path.GetDirectoryName(databasePath);
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(databasePath)) {
                    using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                    connection.Open();
                }
            }
            catch (Exception ex) {
                logger.LogError($"Error creating database: {ex.Message}");
                throw;
            }
        }

        private void EnsureTableExists() {
            try {
                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();

                StringBuilder createTableSql = new StringBuilder();
                createTableSql.AppendLine($"CREATE TABLE IF NOT EXISTS {_tableName} (");
                createTableSql.AppendLine("    Key TEXT PRIMARY KEY");

                foreach (FieldInfo field in fields) {
                    string sqlType = GetSqliteType(field.FieldType);
                    createTableSql.AppendLine($",   {field.Name} {sqlType}");
                }

                createTableSql.Append(")");

                command.CommandText = createTableSql.ToString();
                command.ExecuteNonQuery();
            }
            catch (Exception ex) {
                logger.LogError($"Error creating table: {ex.Message}");
                throw;
            }
        }

        private string GetSqliteType(Type type) {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
                underlyingType == typeof(short) || underlyingType == typeof(byte) ||
                underlyingType == typeof(bool)) {
                return "INTEGER";
            }
            if (underlyingType == typeof(float) || underlyingType == typeof(double) ||
                underlyingType == typeof(decimal)) {
                return "REAL";
            }
            if (underlyingType == typeof(byte[])) {
                return "BLOB";
            }
            if (underlyingType == typeof(DateTime) ||
                underlyingType == typeof(Guid) ||
                underlyingType == typeof(Color) ||
                underlyingType == typeof(string) ||
                underlyingType.IsEnum) {
                return "TEXT";
            }

            return "TEXT";
        }

        private V CreateInstanceFromReader(SqliteDataReader reader) {
            V instance = Activator.CreateInstance<V>();

            foreach (FieldInfo field in fields) {
                try {
                    int ordinal = reader.GetOrdinal(field.Name);

                    if (!reader.IsDBNull(ordinal)) {
                        object value = reader.GetValue(ordinal);
                        Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                        if (targetType == typeof(DateTime) && value is string dateString) {
                            field.SetValue(instance, DateTime.Parse(dateString));
                        }
                        else if (targetType == typeof(Guid) && value is string guidString) {
                            field.SetValue(instance, Guid.Parse(guidString));
                        }
                        else if (targetType == typeof(Color) && value is string colorString) {
                            field.SetValue(instance, colorString.ToColor());
                        }
                        else if (targetType == typeof(bool) && value is long boolValue) {
                            field.SetValue(instance, boolValue != 0);
                        }
                        else if (targetType.IsEnum && value is long enumValue) {
                            field.SetValue(instance, Enum.ToObject(targetType, enumValue));
                        }
                        else if (targetType.IsEnum && value is string enumString) {
                            field.SetValue(instance, Enum.Parse(targetType, enumString));
                        }
                        else if (IsCollectionType(targetType) && value is string jsonString) {
                            object? deserializedValue = JsonConvert.DeserializeObject(jsonString, field.FieldType);
                            field.SetValue(instance, deserializedValue);
                        }
                        else if (value.GetType() != targetType) {
                            field.SetValue(instance, Convert.ChangeType(value, targetType));
                        }
                        else {
                            field.SetValue(instance, value);
                        }
                    }
                }
                catch (Exception ex) {
                    logger.LogWarning($"Error setting field {field.Name}: {ex.Message}");
                }
            }

            return instance;
        }

        private string ConvertKeyToString(T key) {
            if (key is string str) {
                return str;
            }
            return key?.ToString() ?? string.Empty;
        }

        private T ConvertStringToKey(string keyString) {
            Type keyType = typeof(T);

            if (keyType == typeof(string)) {
                return (T)(object)keyString;
            }
            if (keyType == typeof(int)) {
                return (T)(object)int.Parse(keyString);
            }
            if (keyType == typeof(long)) {
                return (T)(object)long.Parse(keyString);
            }
            if (keyType == typeof(Guid)) {
                return (T)(object)Guid.Parse(keyString);
            }

            return (T)Convert.ChangeType(keyString, keyType);
        }

        // Repository-specific methods

        public IEnumerable<V> Query(string sql, params object[] parameters) {
            List<V> results = new List<V>();
            try {
                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = sql;

                for (int i = 0; i < parameters.Length; i++) {
                    command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
                }

                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read()) {
                    V value = CreateInstanceFromReader(reader);
                    if (value != null) {
                        results.Add(value);
                    }
                }
            }
            catch (Exception ex) {
                logger.LogError($"Error executing query: {ex.Message}");
            }
            return results;
        }

        public OperationResult ExecuteCommand(string sql, params object[] parameters) {
            try {
                using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = sql;

                for (int i = 0; i < parameters.Length; i++) {
                    command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
                }

                int rowsAffected = command.ExecuteNonQuery();
                return new OperationResult(true, $"{rowsAffected} row(s) affected", false);
            }
            catch (Exception ex) {
                string errorMessage = $"Error executing command: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }
    }
}
