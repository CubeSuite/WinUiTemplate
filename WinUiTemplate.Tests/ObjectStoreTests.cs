using Microsoft.Data.Sqlite;
using Moq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using WinUiTemplate.Core.Stores;
using WinUiTemplate.Core.Stores.ObjectCache;
using WinUiTemplate.Core.Stores.ObjectStore;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Tests
{
    // Test model for LocalObjectRepository
    public class TestItem
    {
        public int Value { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public Color BackgroundColor { get; set; }
    }

    // Base test class for IObjectCache<T, V>
    public abstract class ObjectStoreTestsBase<TKey, TValue> : IDisposable
    {
        // Services & Stores
        protected readonly Mock<ILoggerService> mockLogger;
        protected readonly Mock<IServiceProvider> mockServiceProvider;
        
        // Constructors

        public ObjectStoreTestsBase() {
            mockLogger = new Mock<ILoggerService>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(mockLogger.Object);
        }

        // Abstract method to create store instance
        protected abstract IObjectCache<TKey, TValue> CreateStore();

        // Tests

        [Fact]
        public void TryAdd_AddsNewItemSuccessfully() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue = GetTestValue(100);

            OperationResult result = store.TryAdd(GetTestKey("key1"), testValue);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(1, store.Count);
            OperationResult getResult = store.TryGet(GetTestKey("key1"), out TValue value);
            Assert.True(getResult.Success);
            AssertValuesEqual(testValue, value);
        }

        [Fact]
        public void TryAdd_ReturnsFailureWhenKeyExists() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue1 = GetTestValue(100);
            TValue testValue2 = GetTestValue(200);

            store.TryAdd(GetTestKey("key1"), testValue1);

            OperationResult result = store.TryAdd(GetTestKey("key1"), testValue2);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("already exists", result.ErrorMessage);
            Assert.Equal(1, store.Count);
            store.TryGet(GetTestKey("key1"), out TValue value);
            AssertValuesEqual(testValue1, value);
        }

        [Fact]
        public void TryUpdate_UpdatesExistingItem() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue1 = GetTestValue(100);
            TValue testValue2 = GetTestValue(200);

            store.TryAdd(GetTestKey("key1"), testValue1);

            OperationResult result = store.TryUpdate(GetTestKey("key1"), testValue2);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(1, store.Count);
            store.TryGet(GetTestKey("key1"), out TValue value);
            AssertValuesEqual(testValue2, value);
        }

        [Fact]
        public void TryUpdate_ReturnsFailureWhenKeyDoesNotExist() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue = GetTestValue(100);

            OperationResult result = store.TryUpdate(GetTestKey("key1"), testValue);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("does not exist", result.ErrorMessage);
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void TryDelete_RemovesExistingItem() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue = GetTestValue(100);

            store.TryAdd(GetTestKey("key1"), testValue);

            OperationResult result = store.TryDelete(GetTestKey("key1"));

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void TryDelete_ReturnsFailureWhenKeyDoesNotExist() {
            IObjectCache<TKey, TValue> store = CreateStore();

            OperationResult result = store.TryDelete(GetTestKey("key1"));

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("does not exist", result.ErrorMessage);
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void TryGet_ReturnsItemWhenKeyExists() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue = GetTestValue(100);

            store.TryAdd(GetTestKey("key1"), testValue);

            OperationResult result = store.TryGet(GetTestKey("key1"), out TValue value);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            AssertValuesEqual(testValue, value);
        }

        [Fact]
        public void TryGet_ReturnsFailureWhenKeyDoesNotExist() {
            IObjectCache<TKey, TValue> store = CreateStore();

            OperationResult result = store.TryGet(GetTestKey("key1"), out TValue value);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("does not exist", result.ErrorMessage);
        }

        [Fact]
        public void ContainsKey_ReturnsTrueWhenKeyExists() {
            IObjectCache<TKey, TValue> store = CreateStore();
            TValue testValue = GetTestValue(100);

            store.TryAdd(GetTestKey("key1"), testValue);

            bool result = store.ContainsKey(GetTestKey("key1"));

            Assert.True(result);
        }

        [Fact]
        public void ContainsKey_ReturnsFalseWhenKeyDoesNotExist() {
            IObjectCache<TKey, TValue> store = CreateStore();

            bool result = store.ContainsKey(GetTestKey("key1"));

            Assert.False(result);
        }

        [Fact]
        public void Clear_RemovesAllItems() {
            IObjectCache<TKey, TValue> store = CreateStore();
            store.TryAdd(GetTestKey("key1"), GetTestValue(100));
            store.TryAdd(GetTestKey("key2"), GetTestValue(200));
            store.TryAdd(GetTestKey("key3"), GetTestValue(300));

            OperationResult result = store.Clear();

            Assert.True(result.Success);
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void Values_ReturnsAllStoredValues() {
            IObjectCache<TKey, TValue> store = CreateStore();
            store.TryAdd(GetTestKey("key1"), GetTestValue(100));
            store.TryAdd(GetTestKey("key2"), GetTestValue(200));
            store.TryAdd(GetTestKey("key3"), GetTestValue(300));

            List<TValue> values = store.Values.ToList();

            Assert.Equal(3, values.Count);
        }

        [Fact]
        public void Keys_ReturnsAllStoredKeys() {
            IObjectCache<TKey, TValue> store = CreateStore();
            store.TryAdd(GetTestKey("key1"), GetTestValue(100));
            store.TryAdd(GetTestKey("key2"), GetTestValue(200));
            store.TryAdd(GetTestKey("key3"), GetTestValue(300));

            List<TKey> keys = store.Keys.ToList();

            Assert.Equal(3, keys.Count);
            Assert.Contains(GetTestKey("key1"), keys);
            Assert.Contains(GetTestKey("key2"), keys);
            Assert.Contains(GetTestKey("key3"), keys);
        }

        [Fact]
        public void Count_ReturnsCorrectCount() {
            IObjectCache<TKey, TValue> store = CreateStore();

            Assert.Equal(0, store.Count);

            store.TryAdd(GetTestKey("key1"), GetTestValue(100));
            Assert.Equal(1, store.Count);

            store.TryAdd(GetTestKey("key2"), GetTestValue(200));
            Assert.Equal(2, store.Count);

            store.TryDelete(GetTestKey("key1"));
            Assert.Equal(1, store.Count);
        }

        // Abstract helper methods for creating test data
        protected abstract TKey GetTestKey(string key);
        protected abstract TValue GetTestValue(int value);
        protected abstract void AssertValuesEqual(TValue expected, TValue actual);

        // Cleanup
        public virtual void Dispose() {
            // Override in derived classes if needed
        }
    }

    // Tests for ObjectCache implementation
    public class ObjectCacheTests : ObjectStoreTestsBase<string, int>
    {
        protected override IObjectCache<string, int> CreateStore() {
            return new ObjectCache<string, int>(mockServiceProvider.Object);
        }

        protected override string GetTestKey(string key) => key;
        protected override int GetTestValue(int value) => value;
        protected override void AssertValuesEqual(int expected, int actual) {
            Assert.Equal(expected, actual);
        }
    }

    // Tests for LocalObjectRepository implementation
    public class LocalObjectRepositoryTests : ObjectStoreTestsBase<string, TestItem>, IClassFixture<TestDatabaseCleanupFixture>
    {
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly string testDatabasePath;
        private readonly string testDatabaseDirectory;
        private static readonly string testRootDirectory = Path.Combine(Path.GetTempPath(), "WinUiTemplate_Tests");

        public LocalObjectRepositoryTests(TestDatabaseCleanupFixture fixture) : base() {
            // Create a unique test database path in a dedicated test directory
            testDatabaseDirectory = Path.Combine(testRootDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDatabaseDirectory);
            testDatabasePath = Path.Combine(testDatabaseDirectory, "test.db");

            mockFilePaths = new Mock<IFilePaths>();
            mockFilePaths.Setup(x => x.Database).Returns(testDatabasePath);

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFilePaths)))
                .Returns(mockFilePaths.Object);
        }

        protected override IObjectCache<string, TestItem> CreateStore() {
            return new LocalObjectRepository<string, TestItem>(mockServiceProvider.Object);
        }

        protected override string GetTestKey(string key) => key;
        protected override TestItem GetTestValue(int value) => new TestItem { Value = value };
        protected override void AssertValuesEqual(TestItem expected, TestItem actual) {
            Assert.Equal(expected.Value, actual.Value);
        }

        // Additional tests specific to database operations

        [Fact]
        public void Constructor_CreatesDatabaseFile() {
            IObjectCache<string, TestItem> store = CreateStore();

            Assert.True(File.Exists(testDatabasePath), "Database file should be created");
        }

        [Fact]
        public void DataPersists_AcrossMultipleStoreInstances() {
            // Create first store and add data
            IObjectCache<string, TestItem> store1 = CreateStore();
            store1.TryAdd("key1", new TestItem { Value = 100 });
            store1.TryAdd("key2", new TestItem { Value = 200 });

            // Create second store and verify data persists
            IObjectCache<string, TestItem> store2 = CreateStore();
            Assert.Equal(2, store2.Count);

            OperationResult result1 = store2.TryGet("key1", out TestItem value1);
            Assert.True(result1.Success);
            Assert.Equal(100, value1.Value);

            OperationResult result2 = store2.TryGet("key2", out TestItem value2);
            Assert.True(result2.Success);
            Assert.Equal(200, value2.Value);
        }

        [Fact]
        public void DatabaseFile_ContainsAddedData() {
            IObjectCache<string, TestItem> store = CreateStore();

            long initialSize = new FileInfo(testDatabasePath).Length;

            // Add data
            for (int i = 0; i < 10; i++) {
                store.TryAdd($"key{i}", new TestItem { Value = i * 100 });
            }

            long finalSize = new FileInfo(testDatabasePath).Length;

            // Verify file exists and data was added
            Assert.True(File.Exists(testDatabasePath));
            Assert.Equal(10, store.Count);
            Assert.True(finalSize >= initialSize, "Database file size should not shrink");
        }

        [Fact]
        public void Clear_KeepsDatabaseFile_ButRemovesData() {
            IObjectCache<string, TestItem> store = CreateStore();
            store.TryAdd("key1", new TestItem { Value = 100 });
            store.TryAdd("key2", new TestItem { Value = 200 });

            OperationResult result = store.Clear();

            Assert.True(result.Success);
            Assert.True(File.Exists(testDatabasePath), "Database file should still exist");
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void MultipleStores_CanAccessSameDatabase_Concurrently() {
            IObjectCache<string, TestItem> store1 = CreateStore();
            IObjectCache<string, TestItem> store2 = CreateStore();

            store1.TryAdd("key1", new TestItem { Value = 100 });

            // Store2 should see the data added by store1
            OperationResult result = store2.TryGet("key1", out TestItem value);

            Assert.True(result.Success);
            Assert.Equal(100, value.Value);
        }

        [Fact]
        public void Update_ModifiesExistingData_InDatabase() {
            // Add data with first store
            IObjectCache<string, TestItem> store1 = CreateStore();
            store1.TryAdd("key1", new TestItem { Value = 100 });

            // Update with second store
            IObjectCache<string, TestItem> store2 = CreateStore();
            store2.TryUpdate("key1", new TestItem { Value = 999 });

            // Verify update persisted with third store
            IObjectCache<string, TestItem> store3 = CreateStore();
            OperationResult result = store3.TryGet("key1", out TestItem value);
            Assert.True(result.Success);
            Assert.Equal(999, value.Value);
        }

        [Fact]
        public void Delete_RemovesData_FromDatabase() {
            // Add data
            IObjectCache<string, TestItem> store1 = CreateStore();
            store1.TryAdd("key1", new TestItem { Value = 100 });
            store1.TryAdd("key2", new TestItem { Value = 200 });

            // Delete one item
            IObjectCache<string, TestItem> store2 = CreateStore();
            store2.TryDelete("key1");

            // Verify deletion persisted
            IObjectCache<string, TestItem> store3 = CreateStore();
            Assert.Equal(1, store3.Count);
            Assert.False(store3.ContainsKey("key1"));
            Assert.True(store3.ContainsKey("key2"));
        }

        public override void Dispose() {
            try {
                SqliteConnection.ClearAllPools();
                Thread.Sleep(10);

                if (Directory.Exists(testDatabaseDirectory)) {
                    Directory.Delete(testDatabaseDirectory, recursive: true);
                }

                if (Directory.Exists(testRootDirectory)) {
                    string[] remainingDirs = Directory.GetDirectories(testRootDirectory);
                    string[] remainingFiles = Directory.GetFiles(testRootDirectory);

                    if (remainingDirs.Length == 0 && remainingFiles.Length == 0) {
                        Directory.Delete(testRootDirectory);
                    }
                }
            } catch { }

            base.Dispose();
        }
    }

    // Fixture for cleaning up test database directories
    public class TestDatabaseCleanupFixture : IDisposable
    {
        private static readonly string testRootDirectory = Path.Combine(Path.GetTempPath(), "WinUiTemplate_Tests");

        public TestDatabaseCleanupFixture() {
            // Clean up all old test directories at the start of test run
            CleanupAllTestDirectories();
        }

        private static void CleanupAllTestDirectories() {
            try {
                SqliteConnection.ClearAllPools();
                Thread.Sleep(50);

                if (Directory.Exists(testRootDirectory)) {
                    for (int attempt = 0; attempt < 3; attempt++) {
                        try {
                            Directory.Delete(testRootDirectory, recursive: true);
                            break;
                        } catch (IOException) when (attempt < 2) {
                            Thread.Sleep(100);
                        }
                    }
                }
            } catch { }
        }

        public void Dispose() {
            // Clean up at the end of all tests
            CleanupAllTestDirectories();
        }
    }

    // Tests for RemoteObjectRepository implementation
    public class RemoteObjectRepositoryTests : ObjectStoreTestsBase<string, TestItem>
    {
        private readonly Mock<IUserSettings> mockUserSettings;
        private readonly string testDatabaseName;
        private readonly string connectionString;
        private readonly TestConfig.PostgreSQLConfig pgConfig;

        public RemoteObjectRepositoryTests() : base() {
            // Load PostgreSQL configuration from testconfig.json
            pgConfig = TestConfig.Instance.PostgreSQL;
            testDatabaseName = $"{pgConfig.Database}_test_{Guid.NewGuid():N}";

            mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.Setup(x => x.DatabaseHost).Returns(pgConfig.Host);
            mockUserSettings.Setup(x => x.DatabasePort).Returns(pgConfig.Port);
            mockUserSettings.Setup(x => x.DatabaseName).Returns(testDatabaseName);
            mockUserSettings.Setup(x => x.DatabaseUsername).Returns(pgConfig.Username);
            mockUserSettings.Setup(x => x.DatabasePassword).Returns(pgConfig.Password);
            mockUserSettings.Setup(x => x.DatabaseConnectionTimeout).Returns(30);

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);

            connectionString = $"Host={pgConfig.Host};Port={pgConfig.Port};Database={testDatabaseName};Username={pgConfig.Username};Password={pgConfig.Password};Timeout=30;MaxPoolSize=20;SslMode=Disable";

            // Create test database
            CreateTestDatabase();
        }

        private void CreateTestDatabase() {
            try {
                string masterConnectionString = $"Host={pgConfig.Host};Port={pgConfig.Port};Database={pgConfig.Database};Username={pgConfig.Username};Password={pgConfig.Password};SslMode=Disable";
                using NpgsqlConnection connection = new NpgsqlConnection(masterConnectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{testDatabaseName}\"";
                command.ExecuteNonQuery();
            } catch (Exception ex) {
                // Database might already exist or PostgreSQL might not be running
                Console.WriteLine($"Failed to create test database: {ex.Message}");
            }
        }

        private void DropTestDatabase() {
            try {
                NpgsqlConnection.ClearAllPools();
                Thread.Sleep(100);

                string masterConnectionString = $"Host={pgConfig.Host};Port={pgConfig.Port};Database={pgConfig.Database};Username={pgConfig.Username};Password={pgConfig.Password};SslMode=Disable";
                using NpgsqlConnection connection = new NpgsqlConnection(masterConnectionString);
                connection.Open();

                using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS \"{testDatabaseName}\" WITH (FORCE)";
                command.ExecuteNonQuery();
            } catch (Exception ex) {
                Console.WriteLine($"Failed to drop test database: {ex.Message}");
            }
        }

        protected override IObjectCache<string, TestItem> CreateStore() {
            try {
                return new RemoteObjectRepository<string, TestItem>(mockServiceProvider.Object);
            } catch (Exception ex) {
                Assert.Fail($"Failed to create RemoteObjectRepository. Ensure PostgreSQL is running at {pgConfig.Host}:{pgConfig.Port} with database '{pgConfig.Database}'. Error: {ex.Message}");
                throw;
            }
        }

        protected override string GetTestKey(string key) => key;

        protected override TestItem GetTestValue(int value) => new TestItem {
            Value = value,
            Name = $"Test Item {value}",
            CreatedAt = DateTime.UtcNow,
            BackgroundColor = Color.FromArgb(255, (byte)(value % 256), (byte)((value * 2) % 256), (byte)((value * 3) % 256))
        };

        protected override void AssertValuesEqual(TestItem expected, TestItem actual) {
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), actual.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            Assert.Equal(expected.BackgroundColor.A, actual.BackgroundColor.A);
            Assert.Equal(expected.BackgroundColor.R, actual.BackgroundColor.R);
            Assert.Equal(expected.BackgroundColor.G, actual.BackgroundColor.G);
            Assert.Equal(expected.BackgroundColor.B, actual.BackgroundColor.B);
        }

        // Additional tests specific to PostgreSQL operations

        [Fact]
        public void Constructor_CreatesTableInDatabase() {
            IObjectCache<string, TestItem> store = CreateStore();

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'TestItems')";
            bool tableExists = (bool)command.ExecuteScalar();

            Assert.True(tableExists, "Table should be created in database");
        }

        [Fact]
        public void DataPersists_AcrossMultipleStoreInstances() {
            IObjectCache<string, TestItem> store1 = CreateStore();
            TestItem item1 = new TestItem { Value = 100, Name = "Item 1", CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0), BackgroundColor = Color.FromArgb(255, 255, 0, 0) };
            TestItem item2 = new TestItem { Value = 200, Name = "Item 2", CreatedAt = new DateTime(2024, 1, 2, 12, 0, 0), BackgroundColor = Color.FromArgb(255, 0, 255, 0) };

            store1.TryAdd("key1", item1);
            store1.TryAdd("key2", item2);

            IObjectCache<string, TestItem> store2 = CreateStore();
            Assert.Equal(2, store2.Count);

            OperationResult result1 = store2.TryGet("key1", out TestItem value1);
            Assert.True(result1.Success);
            AssertValuesEqual(item1, value1);

            OperationResult result2 = store2.TryGet("key2", out TestItem value2);
            Assert.True(result2.Success);
            AssertValuesEqual(item2, value2);
        }

        [Fact]
        public void ColorProperty_SerializesAndDeserializes_Correctly() {
            IObjectCache<string, TestItem> store = CreateStore();
            Color testColor = Color.FromArgb(128, 64, 192, 255);
            TestItem item = new TestItem { Value = 42, Name = "Color Test", CreatedAt = DateTime.UtcNow, BackgroundColor = testColor };

            store.TryAdd("colorKey", item);

            OperationResult result = store.TryGet("colorKey", out TestItem retrieved);

            Assert.True(result.Success);
            Assert.Equal(testColor.A, retrieved.BackgroundColor.A);
            Assert.Equal(testColor.R, retrieved.BackgroundColor.R);
            Assert.Equal(testColor.G, retrieved.BackgroundColor.G);
            Assert.Equal(testColor.B, retrieved.BackgroundColor.B);
        }

        [Fact]
        public void DateTimeProperty_SerializesAndDeserializes_Correctly() {
            IObjectCache<string, TestItem> store = CreateStore();
            DateTime testDate = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
            TestItem item = new TestItem { Value = 123, Name = "DateTime Test", CreatedAt = testDate, BackgroundColor = Color.FromArgb(255, 255, 255, 255) };

            store.TryAdd("dateKey", item);

            OperationResult result = store.TryGet("dateKey", out TestItem retrieved);

            Assert.True(result.Success);
            Assert.Equal(testDate.ToString("yyyy-MM-dd HH:mm:ss"), retrieved.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Fact]
        public void Query_ReturnsFilteredResults() {
            IObjectCache<string, TestItem> store = CreateStore();
            if (store is not RemoteObjectRepository<string, TestItem> repo) {
                Assert.Fail("Store is not RemoteObjectRepository");
                return;
            }

            store.TryAdd("key1", new TestItem { Value = 100, Name = "Item 1", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 255, 0, 0) });
            store.TryAdd("key2", new TestItem { Value = 200, Name = "Item 2", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 0, 255, 0) });
            store.TryAdd("key3", new TestItem { Value = 300, Name = "Item 3", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 0, 0, 255) });

            IEnumerable<TestItem> results = repo.Query("SELECT * FROM \"TestItems\" WHERE \"Value\" > $1", 150);

            List<TestItem> resultList = results.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, item => Assert.True(item.Value > 150));
        }

        [Fact]
        public void ExecuteCommand_ModifiesData() {
            IObjectCache<string, TestItem> store = CreateStore();
            if (store is not RemoteObjectRepository<string, TestItem> repo) {
                Assert.Fail("Store is not RemoteObjectRepository");
                return;
            }

            store.TryAdd("key1", new TestItem { Value = 100, Name = "Item 1", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 255, 0, 0) });
            store.TryAdd("key2", new TestItem { Value = 200, Name = "Item 2", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 0, 255, 0) });

            OperationResult result = repo.ExecuteCommand("UPDATE \"TestItems\" SET \"Value\" = $1 WHERE \"Key\" = $2", 999, "key1");

            Assert.True(result.Success);
            Assert.Contains("1 row(s) affected", result.ErrorMessage);

            store.TryGet("key1", out TestItem updated);
            Assert.Equal(999, updated.Value);
        }

        [Fact]
        public void Clear_RemovesAllData_ButKeepsTable() {
            IObjectCache<string, TestItem> store = CreateStore();
            store.TryAdd("key1", new TestItem { Value = 100, Name = "Item 1", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 255, 0, 0) });
            store.TryAdd("key2", new TestItem { Value = 200, Name = "Item 2", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 0, 255, 0) });

            OperationResult result = store.Clear();

            Assert.True(result.Success);
            Assert.Equal(0, store.Count);

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'TestItems')";
            bool tableExists = (bool)command.ExecuteScalar();

            Assert.True(tableExists, "Table should still exist after Clear");
        }

        [Fact]
        public void MultipleStores_CanAccessSameDatabase_Concurrently() {
            IObjectCache<string, TestItem> store1 = CreateStore();
            IObjectCache<string, TestItem> store2 = CreateStore();

            TestItem item = new TestItem { Value = 100, Name = "Concurrent Test", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 128, 128, 128) };
            store1.TryAdd("key1", item);

            OperationResult result = store2.TryGet("key1", out TestItem value);

            Assert.True(result.Success);
            AssertValuesEqual(item, value);
        }

        [Fact]
        public void Update_ModifiesExistingData_InDatabase() {
            IObjectCache<string, TestItem> store1 = CreateStore();
            TestItem original = new TestItem { Value = 100, Name = "Original", CreatedAt = new DateTime(2024, 1, 1), BackgroundColor = Color.FromArgb(255, 255, 0, 0) };
            store1.TryAdd("key1", original);

            IObjectCache<string, TestItem> store2 = CreateStore();
            TestItem updated = new TestItem { Value = 999, Name = "Updated", CreatedAt = new DateTime(2024, 12, 31), BackgroundColor = Color.FromArgb(255, 0, 0, 255) };
            store2.TryUpdate("key1", updated);

            IObjectCache<string, TestItem> store3 = CreateStore();
            OperationResult result = store3.TryGet("key1", out TestItem value);

            Assert.True(result.Success);
            AssertValuesEqual(updated, value);
        }

        [Fact]
        public void Delete_RemovesData_FromDatabase() {
            IObjectCache<string, TestItem> store1 = CreateStore();
            store1.TryAdd("key1", new TestItem { Value = 100, Name = "Item 1", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 255, 0, 0) });
            store1.TryAdd("key2", new TestItem { Value = 200, Name = "Item 2", CreatedAt = DateTime.UtcNow, BackgroundColor = Color.FromArgb(255, 0, 255, 0) });

            IObjectCache<string, TestItem> store2 = CreateStore();
            store2.TryDelete("key1");

            IObjectCache<string, TestItem> store3 = CreateStore();
            Assert.Equal(1, store3.Count);
            Assert.False(store3.ContainsKey("key1"));
            Assert.True(store3.ContainsKey("key2"));
        }

        [Fact]
        public void LargeDataSet_HandledCorrectly() {
            IObjectCache<string, TestItem> store = CreateStore();

            for (int i = 0; i < 100; i++) {
                store.TryAdd($"key{i}", new TestItem {
                    Value = i,
                    Name = $"Item {i}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    BackgroundColor = Color.FromArgb(255, (byte)(i % 256), (byte)((i * 2) % 256), (byte)((i * 3) % 256))
                });
            }

            Assert.Equal(100, store.Count);

            List<TestItem> allValues = store.Values.ToList();
            Assert.Equal(100, allValues.Count);

            List<string> allKeys = store.Keys.ToList();
            Assert.Equal(100, allKeys.Count);
        }

        public override void Dispose() {
            try {
                DropTestDatabase();
            } catch { }

            base.Dispose();
        }
    }

    // Test class with unsupported type for validation tests
    public class UnsupportedTestItem
    {
        public int Value { get; set; }
        public System.Collections.Generic.Dictionary<string, string> UnsupportedProperty { get; set; } // Not supported
    }

    // Tests for type validation in object repositories
    public class ObjectRepositoryTypeValidationTests : IDisposable
    {
        private readonly Mock<ILoggerService> mockLogger;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IUserSettings> mockUserSettings;
        private readonly string testDatabasePath;

        public ObjectRepositoryTypeValidationTests() {
            mockLogger = new Mock<ILoggerService>();
            mockServiceProvider = new Mock<IServiceProvider>();
            mockFilePaths = new Mock<IFilePaths>();
            mockUserSettings = new Mock<IUserSettings>();

            testDatabasePath = Path.Combine(Path.GetTempPath(), $"validation_test_{Guid.NewGuid():N}.db");

            mockFilePaths.Setup(x => x.Database).Returns(testDatabasePath);
            mockUserSettings.Setup(x => x.DatabaseHost).Returns("localhost");
            mockUserSettings.Setup(x => x.DatabasePort).Returns(5432);
            mockUserSettings.Setup(x => x.DatabaseName).Returns("test");
            mockUserSettings.Setup(x => x.DatabaseUsername).Returns("test");
            mockUserSettings.Setup(x => x.DatabasePassword).Returns("test");
            mockUserSettings.Setup(x => x.DatabaseConnectionTimeout).Returns(30);

            mockServiceProvider
                .Setup(x => x.GetService(typeof(ILoggerService)))
                .Returns(mockLogger.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IFilePaths)))
                .Returns(mockFilePaths.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);
        }

        [Fact]
        public void LocalObjectRepository_ThrowsNotSupportedException_ForUnsupportedTypes() {
            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => {
                new LocalObjectRepository<string, UnsupportedTestItem>(mockServiceProvider.Object);
            });

            Assert.Contains("UnsupportedProperty", exception.Message);
            Assert.Contains("Dictionary", exception.Message);
            Assert.Contains("Supported types:", exception.Message);
        }

        [Fact]
        public void RemoteObjectRepository_ThrowsNotSupportedException_ForUnsupportedTypes() {
            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => {
                new RemoteObjectRepository<string, UnsupportedTestItem>(mockServiceProvider.Object);
            });

            Assert.Contains("UnsupportedProperty", exception.Message);
            Assert.Contains("Dictionary", exception.Message);
            Assert.Contains("Supported types:", exception.Message);
        }

        public void Dispose() {
            try {
                if (File.Exists(testDatabasePath)) {
                    File.Delete(testDatabasePath);
                }
            } catch { }
        }
    }
}

