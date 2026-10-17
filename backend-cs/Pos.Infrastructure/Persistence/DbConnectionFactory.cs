using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PosCs.Infrastructure.Config;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>SQLite connection factory. Database lives in the data/ folder next to the executable.</summary>
    public static class DbConnectionFactory
    {
        private static string _dbPath;

        public static string DbPath
        {
            get
            {
                if (_dbPath == null)
                {
                    var overridePath = PosConfig.Get("POS_DB_PATH");
                    if (!string.IsNullOrWhiteSpace(overridePath))
                    {
                        var directory = Path.GetDirectoryName(overridePath);
                        if (!string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);
                        _dbPath = overridePath;
                    }
                    else
                    {
                        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                        Directory.CreateDirectory(dataDir);
                        _dbPath = Path.Combine(dataDir, "dev.db");
                    }
                }
                return _dbPath;
            }
        }

        public static string ConnectionString => $"Data Source={DbPath}";

        public static void UseDatabase(string path)
        {
            _dbPath = path;
        }

        public static SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
