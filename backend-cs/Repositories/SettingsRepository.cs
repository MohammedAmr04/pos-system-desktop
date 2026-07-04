using System;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class SettingsRepository
    {
        public Settings GetByMachineId(SqliteConnection conn, string machineId)
        {
            return conn.QueryFirstOrDefault<Settings>(
                "SELECT * FROM Settings WHERE machineId = @machineId", new { machineId });
        }

        public Settings GetFirst(SqliteConnection conn)
        {
            return conn.QueryFirstOrDefault<Settings>("SELECT * FROM Settings LIMIT 1");
        }

        public Settings Create(SqliteConnection conn, string machineId, bool unlocked = false)
        {
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid().ToString("N");
            conn.Execute(@"
                INSERT INTO Settings (id, machineId, activatedAt, lastCheckedAt, unlocked, createdAt, updatedAt)
                VALUES (@id, @machineId, @activatedAt, @lastCheckedAt, @unlocked, @createdAt, @updatedAt)",
                new
                {
                    id,
                    machineId,
                    activatedAt = now,
                    lastCheckedAt = now,
                    unlocked = unlocked ? 1 : 0,
                    createdAt = now,
                    updatedAt = now
                });
            return new Settings
            {
                Id = id,
                MachineId = machineId,
                ActivatedAt = now,
                LastCheckedAt = now,
                Unlocked = unlocked,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public void SetUnlocked(SqliteConnection conn, string machineId)
        {
            conn.Execute(@"
                UPDATE Settings SET unlocked = 1, lastCheckedAt = @now, updatedAt = @now
                WHERE machineId = @machineId",
                new { now = DateTime.UtcNow, machineId });
        }

        public void Upsert(SqliteConnection conn, string machineId, bool unlocked)
        {
            var existing = GetByMachineId(conn, machineId);
            if (existing != null)
            {
                conn.Execute(@"
                    UPDATE Settings SET unlocked = @unlocked, lastCheckedAt = @now, updatedAt = @now
                    WHERE machineId = @machineId",
                    new { unlocked = unlocked ? 1 : 0, now = DateTime.UtcNow, machineId });
            }
            else
            {
                Create(conn, machineId, unlocked);
            }
        }
    }
}
