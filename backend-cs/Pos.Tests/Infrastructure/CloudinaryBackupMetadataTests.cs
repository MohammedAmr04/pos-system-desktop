using System;
using PosCs.Infrastructure.Persistence;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    public class CloudinaryBackupMetadataTests
    {
        [Fact]
        public void BuildMetadataContext_IncludesSafeBackupDetails()
        {
            var context = CloudinaryBackupUploader.BuildMetadataContext(
                "Default Restaurant",
                "tenant-default",
                1048576,
                new DateTime(2026, 9, 12, 3, 30, 0));

            Assert.Equal(
                "tenant_name=Default Restaurant|tenant_id=tenant-default|backup_date=2026-09-12T03:30:00|app_version=v2|database_size_bytes=1048576",
                context);
        }
    }
}
