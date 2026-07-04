using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PosCs.Helpers
{
    public static class DbConnectionFactory
    {
        private static string _dbPath;

        public static string DbPath
        {
            get
            {
                if (_dbPath == null)
                {
                    var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    Directory.CreateDirectory(dataDir);
                    _dbPath = Path.Combine(dataDir, "dev.db");
                }
                return _dbPath;
            }
        }

        public static string ConnectionString => $"Data Source={DbPath}";

        public static SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
