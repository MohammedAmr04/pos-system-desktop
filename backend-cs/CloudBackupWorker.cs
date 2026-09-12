using System;
using System.IO;
using System.Linq;
using System.Threading;
using PosCs.Infrastructure.Persistence;

namespace PosCs
{
    internal sealed class CloudBackupWorker : IDisposable
    {
        private readonly BackupService _backupService;
        private readonly CloudinaryBackupUploader _uploader;
        private readonly Timer _timer;
        private string _lastUploadedPath;
        private int _running;

        public CloudBackupWorker(BackupService backupService, CloudinaryBackupUploader uploader)
        {
            _backupService = backupService;
            _uploader = uploader;
            var intervalMinutes = ParseInterval(Environment.GetEnvironmentVariable("POS_BACKUP_INTERVAL_MINUTES"));
            _timer = new Timer(Run, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(intervalMinutes));
        }

        private void Run(object state)
        {
            if (Interlocked.Exchange(ref _running, 1) == 1) return;
            try
            {
                var latest = EnsureDailyLocalBackup();
                if (latest == null || !_uploader.IsConfigured || string.Equals(latest, _lastUploadedPath, StringComparison.OrdinalIgnoreCase))
                    return;

                _uploader.UploadLatest(latest);
                _lastUploadedPath = latest;
                Console.WriteLine("[BACKUP] Latest backup uploaded to Cloudinary: " + Path.GetFileName(latest));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[BACKUP WARN] Automatic backup sync failed: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private string EnsureDailyLocalBackup()
        {
            var backups = _backupService.List()
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
            var today = DateTime.Now.Date;
            if (backups.Any(item => item.CreatedAt.ToLocalTime().Date == today))
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backups", backups[0].FileName);

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backups", _backupService.Create(null).FileName);
        }

        private static int ParseInterval(string value)
        {
            int minutes;
            if (!int.TryParse(value, out minutes) || minutes < 5 || minutes > 1440)
                return 15;
            return minutes;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
