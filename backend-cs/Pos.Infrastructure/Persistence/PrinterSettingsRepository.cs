using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Infrastructure.Persistence
{
    public class PrinterSettingsRepository : IPrinterSettingsRepository
    {
        public PrinterSettings Get()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var rows = conn.Query<SettingRow>(
                    "SELECT key, value FROM PrinterSetting").ToList();
                return PrinterSettings.FromDictionary(
                    rows.ToDictionary(r => r.key, r => r.value));
            }
        }

        public void Save(PrinterSettings settings)
        {
            settings.Normalize();
            var map = settings.ToDictionary();
            using (var conn = DbConnectionFactory.CreateConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    conn.Execute("DELETE FROM PrinterSetting", transaction: tx);
                    foreach (var kv in map)
                    {
                        conn.Execute(
                            @"INSERT INTO PrinterSetting (key, value, updatedAt)
                              VALUES (@key, @value, @updatedAt)",
                            new { key = kv.Key, value = kv.Value, updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                            transaction: tx);
                    }
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private sealed class SettingRow
        {
            public string key { get; set; }
            public string value { get; set; }
        }
    }
}
