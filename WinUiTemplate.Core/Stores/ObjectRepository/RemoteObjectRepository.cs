using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using WinUiTemplate.Core.Services.Interfaces;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Core.Stores
{
    public class RemoteObjectRepository<T, V> : IObjectRepository<T, V>
    {
        // Services & Stores
        private readonly ILoggerService logger;
        private readonly IUserSettings userSettings;
        private readonly string _tableName;
        private readonly FieldInfo[] fields;
        private readonly string connectionString;

        // Properties
        public string TableName => _tableName;

        public IEnumerable<V> Values {
            get {
                List<V> values = new List<V>();
                try {
                    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();

                    using NpgsqlCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT * FROM {_tableName}";

                    using NpgsqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        V value = CreateInstanceFromReader(reader);
                        if (value != null) {
                            values.Add(value);
                        }
                    }
                } catch (Exception ex) {
                    logger.LogError($"Error retrieving values: {ex.Message}");
                }
                return values;
            }
        }

        public IEnumerable<T> Keys {
            get {
                List<T> keys = new List<T>();
                try {
                    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();

                    using NpgsqlCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT \"Key\" FROM {_tableName}";

                    using NpgsqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        string keyString = reader.GetString(0);
                        T key = ConvertStringToKey(keyString);
                        keys.Add(key);
                    }
                } catch (Exception ex) {
                    logger.LogError($"Error retrieving keys: {ex.Message}");
                }
                return keys;
            }
        }

        public int Count {
            get {
                try {
                    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();

                    using NpgsqlCommand command = connection.CreateCommand();
                    command.CommandText = $"SELECT COUNT(*) FROM {_tableName}";

                    object result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                } catch (Exception ex) {
                    logger.LogError($"Error retrieving count: {ex.Message}");
                    return 0;
                }
            }
        }

        // Constructors

        public RemoteObjectRepository(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();

            _tableName = $"\"{typeof(V).Name}s\"";
            fields = typeof(V).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .ToArray();

            ValidateFieldTypes();
            connectionString = BuildConnectionString();
            EnsureTableExists();
        }

        // Public Functions

        public OperationResult TryAdd(T key, V instance) {
            try {
                string keyString = ConvertKeyToString(key);

                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();

                StringBuilder columns = new StringBuilder("\"Key\"");
                StringBuilder values = new StringBuilder("@key");
                StringBuilder placeholders = new StringBuilder("$1");
                int parameterIndex = 2;

                foreach (FieldInfo field in fields) {
                    columns.Append($", \"{field.Name}\"");
                    placeholders.Append($", ${parameterIndex}");
                    parameterIndex++;
                }

                command.CommandText = $"INSERT INTO {_tableName} ({columns}) VALUES ({placeholders})";
                command.Parameters.AddWithValue(keyString);

                foreach (FieldInfo field in fields) {
                    object? value = field.GetValue(instance);
                    Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                    if (targetType == typeof(Color) && value is Color colorValue) {
                        command.Parameters.AddWithValue(colorValue.ToHex());
                    }
                    else if (IsCollectionType(targetType) && value != null) {
                        command.Parameters.AddWithValue(JsonConvert.SerializeObject(value));
                    }
                    else {
                        command.Parameters.AddWithValue(value ?? DBNull.Value);
                    }
                }

                command.ExecuteNonQuery();

                return new OperationResult(true, null, false);
            } catch (PostgresException ex) when (ex.SqlState == "23505") {
                string errorMessage = $"Key '{key}' already exists in repository";
                logger.LogWarning(errorMessage);
                return new OperationResult(false, errorMessage, false);
            } catch (Exception ex) {
                string errorMessage = $"Error adding item to repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryUpdate(T key, V instance) {
            try {
                string keyString = ConvertKeyToString(key);

                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();

                StringBuilder setClause = new StringBuilder();
                int parameterIndex = 1;

                for (int i = 0; i < fields.Length; i++) {
                    if (i > 0) setClause.Append(", ");
                    setClause.Append($"\"{fields[i].Name}\" = ${parameterIndex}");
                    parameterIndex++;
                }

                command.CommandText = $"UPDATE {_tableName} SET {setClause} WHERE \"Key\" = ${parameterIndex}";

                foreach (FieldInfo field in fields) {
                    object value = field.GetValue(instance);
                    Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                    if (targetType == typeof(Color) && value is Color colorValue) {
                        command.Parameters.AddWithValue(colorValue.ToHex());
                    }
                    else if (IsCollectionType(targetType) && value != null) {
                        command.Parameters.AddWithValue(JsonConvert.SerializeObject(value));
                    }
                    else {
                        command.Parameters.AddWithValue(value ?? DBNull.Value);
                    }
                }
                command.Parameters.AddWithValue(keyString);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0) {
                    string errorMessage = $"Key '{key}' does not exist in repository";
                    logger.LogWarning(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                return new OperationResult(true, null, false);
            } catch (Exception ex) {
                string errorMessage = $"Error updating item in repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryDelete(T key) {
            try {
                string keyString = ConvertKeyToString(key);

                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {_tableName} WHERE \"Key\" = $1";
                command.Parameters.AddWithValue(keyString);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0) {
                    string errorMessage = $"Key '{key}' does not exist in repository";
                    logger.LogError(errorMessage);
                    return new OperationResult(false, errorMessage, false);
                }

                return new OperationResult(true, null, false);
            } catch (Exception ex) {
                string errorMessage = $"Error deleting item from repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public OperationResult TryGet(T key, out V value) {
            value = default(V);

            try {
                string keyString = ConvertKeyToString(key);

                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {_tableName} WHERE \"Key\" = $1";
                command.Parameters.AddWithValue(keyString);

                using NpgsqlDataReader reader = command.ExecuteReader();
                if (reader.Read()) {
                    value = CreateInstanceFromReader(reader);
                    return new OperationResult(true, null, false);
                }

                string errorMessage = $"Key '{key}' does not exist in repository";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            } catch (Exception ex) {
                string errorMessage = $"Error retrieving item from repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        public bool ContainsKey(T key) {
            try {
                string keyString = ConvertKeyToString(key);

                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {_tableName} WHERE \"Key\" = $1";
                command.Parameters.AddWithValue(keyString);

                object result = command.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            } catch (Exception ex) {
                logger.LogError($"Error checking if key exists: {ex.Message}");
                return false;
            }
        }

        public virtual OperationResult Clear() {
            try {
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {_tableName}";
                command.ExecuteNonQuery();

                return new OperationResult(true, null, false);
            } catch (Exception ex) {
                string errorMessage = $"Error clearing repository: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }

        // Private Functions

        private void ValidateFieldTypes() {
            List<string> unsupportedProperties = new List<string>();

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
                    unsupportedProperties.Add($"{field.Name} ({targetType.Name})");
                }
            }

            if (unsupportedProperties.Count > 0) {
                string errorMessage = $"Type '{typeof(V).Name}' contains unsupported property types: {string.Join(", ", unsupportedProperties)}. " +
                    "Supported types: int, long, short, byte, bool, float, double, decimal, byte[], DateTime, Guid, Color, string, enums, and collections (List, Array, HashSet, Dictionary).";
                logger.LogError(errorMessage);
                throw new NotSupportedException(errorMessage);
            }
        }

        private string BuildConnectionString() {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder {
                Host = userSettings.DatabaseHost,
                Port = userSettings.DatabasePort,
                Database = userSettings.DatabaseName,
                Username = userSettings.DatabaseUsername,
                Password = userSettings.DatabasePassword,
                Timeout = userSettings.DatabaseConnectionTimeout,
                MaxPoolSize = 20,
                SslMode = SslMode.Disable
            };

            return builder.ToString();
        }

        private void EnsureTableExists() {
            try {
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();

                StringBuilder createTableSql = new StringBuilder();
                createTableSql.AppendLine($"CREATE TABLE IF NOT EXISTS {_tableName} (");
                createTableSql.AppendLine("    \"Key\" TEXT PRIMARY KEY");

                foreach (FieldInfo prop in fields) {
                    string sqlType = GetPostgresType(prop.FieldType);
                    createTableSql.AppendLine($",   \"{prop.Name}\" {sqlType}");
                }

                createTableSql.Append(")");

                command.CommandText = createTableSql.ToString();
                command.ExecuteNonQuery();
            } catch (Exception ex) {
                logger.LogError($"Error creating table: {ex.Message}");
                throw;
            }
        }

        private string GetPostgresType(Type type) {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int)) return "INTEGER";
            if (underlyingType == typeof(long)) return "BIGINT";
            if (underlyingType == typeof(short)) return "SMALLINT";
            if (underlyingType == typeof(byte)) return "SMALLINT";
            if (underlyingType == typeof(bool)) return "BOOLEAN";
            if (underlyingType == typeof(float)) return "REAL";
            if (underlyingType == typeof(double)) return "DOUBLE PRECISION";
            if (underlyingType == typeof(decimal)) return "NUMERIC";
            if (underlyingType == typeof(byte[])) return "BYTEA";
            if (underlyingType == typeof(DateTime)) return "TIMESTAMP";
            if (underlyingType == typeof(Guid)) return "UUID";
            if (underlyingType == typeof(Color)) return "TEXT";
            if (underlyingType == typeof(string)) return "TEXT";
            if (underlyingType.IsEnum) return "TEXT";

            return "TEXT";
        }

        private bool IsCollectionType(Type type) {
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

        private V CreateInstanceFromReader(NpgsqlDataReader reader) {
            V instance = Activator.CreateInstance<V>();

            foreach (FieldInfo field in fields) {
                try {
                    int ordinal = reader.GetOrdinal(field.Name);

                    if (!reader.IsDBNull(ordinal)) {
                        object value = reader.GetValue(ordinal);
                        Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                        if (targetType.IsEnum && value is string enumString) {
                            field.SetValue(instance, Enum.Parse(targetType, enumString));
                        }
                        else if (targetType == typeof(Guid) && value is string guidString) {
                            field.SetValue(instance, Guid.Parse(guidString));
                        }
                        else if (targetType == typeof(Color) && value is string colorString) {
                            field.SetValue(instance, colorString.ToColor());
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
                } catch (Exception ex) {
                    logger.LogWarning($"Error setting property {field.Name}: {ex.Message}");
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
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = sql;

                for (int i = 0; i < parameters.Length; i++) {
                    command.Parameters.AddWithValue(parameters[i] ?? DBNull.Value);
                }

                using NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) {
                    V value = CreateInstanceFromReader(reader);
                    if (value != null) {
                        results.Add(value);
                    }
                }
            } catch (Exception ex) {
                logger.LogError($"Error executing query: {ex.Message}");
            }
            return results;
        }

        public OperationResult ExecuteCommand(string sql, params object[] parameters) {
            try {
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = sql;

                for (int i = 0; i < parameters.Length; i++) {
                    command.Parameters.AddWithValue(parameters[i] ?? DBNull.Value);
                }

                int rowsAffected = command.ExecuteNonQuery();
                return new OperationResult(true, $"{rowsAffected} row(s) affected", false);
            } catch (Exception ex) {
                string errorMessage = $"Error executing command: {ex.Message}";
                logger.LogError(errorMessage);
                return new OperationResult(false, errorMessage, false);
            }
        }
    }
}
