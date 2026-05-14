using Newtonsoft.Json;
using System;
using System.IO;

namespace WinUiTemplate.Tests
{
    public class TestConfig
    {
        public PostgreSQLConfig PostgreSQL { get; set; }

        public class PostgreSQLConfig
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Database { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private static TestConfig _instance;
        private static readonly object _lock = new object();

        public static TestConfig Instance {
            get {
                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        private static TestConfig Load() {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testconfig.json");

            if (!File.Exists(configPath)) {
                throw new FileNotFoundException(
                    $"Test configuration file not found at '{configPath}'. " +
                    "Please create testconfig.json based on testconfig.example.json and fill in your PostgreSQL credentials."
                );
            }

            string json = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<TestConfig>(json);
        }
    }
}
