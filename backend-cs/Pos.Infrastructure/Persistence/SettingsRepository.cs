using System;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class SettingsRepository : ILicenseRepository
    {
        public Settings GetByMachineId(string machineId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetByMachineIdCore(conn, machineId);
        }

        public Settings GetFirst()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetFirstCore(conn);
        }

        public Settings Create(string machineId, bool unlocked = false)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return CreateCore(conn, machineId, unlocked);
        }

        public void Upsert(string machineId, bool unlocked)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                UpsertCore(conn, machineId, unlocked);
        }

        internal static Settings GetByMachineIdCore(SqliteConnection conn, string machineId)
        {
            return conn.QueryFirstOrDefault<Settings>(
                "SELECT * FROM Settings WHERE machineId = @machineId", new { machineId });
        }

        internal static Settings GetFirstCore(SqliteConnection conn)
        {
            return conn.QueryFirstOrDefault<Settings>("SELECT * FROM Settings LIMIT 1");
        }

        internal static Settings CreateCore(SqliteConnection conn, string machineId, bool unlocked = false)
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

        internal static void UpsertCore(SqliteConnection conn, string machineId, bool unlocked)
        {
            var existing = GetByMachineIdCore(conn, machineId);
            if (existing != null)
            {
                conn.Execute(@"
                    UPDATE Settings SET unlocked = @unlocked, lastCheckedAt = @now, updatedAt = @now
                    WHERE machineId = @machineId",
                    new { unlocked = unlocked ? 1 : 0, now = DateTime.UtcNow, machineId });
            }
            else
            {
                CreateCore(conn, machineId, unlocked);
            }
        }
    }
}
