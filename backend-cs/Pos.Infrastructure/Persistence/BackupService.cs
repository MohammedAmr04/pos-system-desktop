using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Models;

namespace PosCs.Infrastructure.Persistence
{
    public class BackupService
    {
        private readonly string _backupDirectory;

        public BackupService()
        {
            _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backups");
            Directory.CreateDirectory(_backupDirectory);
        }

        public IList<BackupSummary> List()
        {
            return Directory.GetFiles(_backupDirectory, "*.db", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => !file.Name.StartsWith("pre-restore-", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.CreationTimeUtc)
                .Select(file => new BackupSummary
                {
                    FileName = file.Name,
                    SizeBytes = file.Length,
                    CreatedAt = file.CreationTimeUtc
                })
                .ToList();
        }

        public BackupSummary Create(string actorUserId)
        {
            var fileName = "pos-v2-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".db";
            var destination = Path.Combine(_backupDirectory, fileName);
            File.Copy(DbConnectionFactory.DbPath, destination, false);
            ValidateDatabase(destination);
            WriteAudit(actorUserId, "backup.create", fileName, "Created database backup");
            return ToSummary(destination);
        }

        public BackupSummary Restore(string fileName, string actorUserId)
        {
            var source = ResolveBackupPath(fileName);
            ValidateDatabase(source);

            var safetyBackup = Path.Combine(_backupDirectory,
                "pre-restore-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".db");
            File.Copy(DbConnectionFactory.DbPath, safetyBackup, false);

            var temporary = Path.Combine(Path.GetDirectoryName(DbConnectionFactory.DbPath),
                "restore-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.Copy(source, temporary, true);
                ValidateDatabase(temporary);
                File.Copy(temporary, DbConnectionFactory.DbPath, true);
            }
            finally
            {
                TryDeleteTemporaryFile(temporary);
            }

            WriteAudit(actorUserId, "backup.restore", fileName, "Restored database backup");
            return ToSummary(safetyBackup);
        }

        private string ResolveBackupPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName || !fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid backup file name");

            var path = Path.Combine(_backupDirectory, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException("Backup not found");
            return path;
        }

        private static void ValidateDatabase(string path)
        {
            using (var connection = new SqliteConnection("Data Source=" + path + ";Pooling=False"))
            {
                connection.Open();
                var result = connection.ExecuteScalar<string>("PRAGMA integrity_check;");
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Backup database integrity check failed");
            }
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("[BACKUP WARN] Could not remove temporary restore file: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine("[BACKUP WARN] Could not remove temporary restore file: " + ex.Message);
            }
        }

        private void WriteAudit(string actorUserId, string action, string fileName, string summary)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                connection.Execute(@"
                    INSERT INTO AuditLog (id, actorUserId, action, entityType, entityId, summary, createdAt)
                    VALUES (@id, @actorUserId, @action, 'database', @fileName, @summary, @createdAt)",
                    new
                    {
                        id = Guid.NewGuid().ToString("N"),
                        actorUserId,
                        action,
                        fileName,
                        summary,
                        createdAt = DateTime.UtcNow
                    });
            }
        }

        private static BackupSummary ToSummary(string path)
        {
            var file = new FileInfo(path);
            return new BackupSummary { FileName = file.Name, SizeBytes = file.Length, CreatedAt = file.CreationTimeUtc };
        }
    }
}
