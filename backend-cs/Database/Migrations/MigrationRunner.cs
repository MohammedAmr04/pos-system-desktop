using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;

namespace PosCs.Database.Migrations
{
    public static class MigrationRunner
    {
        public static void ApplyPending(SqliteConnection conn)
        {
            // Create tracking table
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS __Migrations (
                    id TEXT PRIMARY KEY,
                    applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )");

            var applied = new HashSet<string>(
                conn.Query<string>("SELECT id FROM __Migrations") ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase
            );

            var migrationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
            if (!Directory.Exists(migrationsDir))
            {
                Console.WriteLine("[MIGRATIONS] No migrations directory found");
                return;
            }

            var migrationFiles = Directory.GetFiles(migrationsDir, "*.sql")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            Console.WriteLine($"[MIGRATIONS] Found {migrationFiles.Count} migration files");

            foreach (var file in migrationFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (applied.Contains(name))
                {
                    Console.WriteLine($"[MIGRATIONS] Skipping already applied: {name}");
                    continue;
                }

                var sql = File.ReadAllText(file);
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var statements = SplitSql(sql);
                        foreach (var stmt in statements)
                        {
                            if (!string.IsNullOrWhiteSpace(stmt))
                                conn.Execute(stmt, transaction: tx);
                        }
                        conn.Execute("INSERT INTO __Migrations (id) VALUES (@name)", new { name }, transaction: tx);
                        tx.Commit();
                        Console.WriteLine($"[MIGRATIONS] Applied: {name}");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.Error.WriteLine($"[MIGRATIONS] Failed '{name}': {ex.Message}");
                        throw;
                    }
                }
            }
        }

        private static List<string> SplitSql(string sql)
        {
            var statements = new List<string>();
            var current = new System.Text.StringBuilder();
            foreach (var line in sql.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("--") || trimmed.StartsWith("#"))
                    continue;
                current.AppendLine(line);
                if (trimmed.EndsWith(";"))
                {
                    statements.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
            if (current.ToString().Trim().Length > 0)
                statements.Add(current.ToString().Trim());
            return statements;
        }
    }
}
