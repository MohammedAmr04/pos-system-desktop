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

        public void RecordLastSeenDate(string machineId, DateTime date)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                conn.Execute(@"
                    UPDATE Settings SET lastSeenDate = @date, lastCheckedAt = @now, updatedAt = @now
                    WHERE machineId = @machineId",
                    new { machineId, date = date.Date, now = DateTime.UtcNow });
            }
        }

        public void SaveLicenseConfiguration(string machineId, string licenseType, int trialDays,
            DateTime startedAt, DateTime? expiresAt)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                conn.Execute(@"
                    UPDATE Settings
                    SET licenseType = @licenseType, trialDays = @trialDays,
                        licenseStartedAt = @startedAt, licenseExpiresAt = @expiresAt,
                        unlocked = CASE WHEN @licenseType = 'permanent' THEN 1 ELSE unlocked END,
                        updatedAt = @now
                    WHERE machineId = @machineId",
                    new { machineId, licenseType, trialDays, startedAt = startedAt.Date, expiresAt, now = DateTime.UtcNow });
            }
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
            var startedAt = DateTime.Today;
            var expiresAt = startedAt.AddDays(14);
            var id = Guid.NewGuid().ToString("N");
            conn.Execute(@"
                INSERT INTO Settings (id, machineId, activatedAt, lastCheckedAt, lastSeenDate, unlocked,
                    licenseType, trialDays, licenseStartedAt, licenseExpiresAt, createdAt, updatedAt)
                VALUES (@id, @machineId, @activatedAt, @lastCheckedAt, @lastSeenDate, @unlocked,
                    @licenseType, @trialDays, @licenseStartedAt, @licenseExpiresAt, @createdAt, @updatedAt)",
                new
                {
                    id,
                    machineId,
                    activatedAt = now,
                    lastCheckedAt = now,
                    lastSeenDate = startedAt,
                    unlocked = unlocked ? 1 : 0,
                    licenseType = unlocked ? "permanent" : "trial",
                    trialDays = 14,
                    licenseStartedAt = startedAt,
                    licenseExpiresAt = unlocked ? (DateTime?)null : expiresAt,
                    createdAt = now,
                    updatedAt = now
                });
            return new Settings
            {
                Id = id,
                MachineId = machineId,
                ActivatedAt = now,
                LastCheckedAt = now,
                LastSeenDate = startedAt,
                Unlocked = unlocked,
                LicenseType = unlocked ? "permanent" : "trial",
                TrialDays = 14,
                LicenseStartedAt = startedAt,
                LicenseExpiresAt = unlocked ? (DateTime?)null : expiresAt,
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
                    UPDATE Settings SET unlocked = @unlocked, licenseType = @licenseType,
                        licenseExpiresAt = @licenseExpiresAt, lastCheckedAt = @now, updatedAt = @now
                    WHERE machineId = @machineId",
                    new
                    {
                        unlocked = unlocked ? 1 : 0,
                        licenseType = unlocked ? "permanent" : "trial",
                        licenseExpiresAt = unlocked ? (DateTime?)null : existing.LicenseExpiresAt,
                        now = DateTime.UtcNow,
                        machineId
                    });
            }
            else
            {
                CreateCore(conn, machineId, unlocked);
            }
        }
    }
}
