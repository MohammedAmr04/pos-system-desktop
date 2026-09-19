using System;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Infrastructure.Data;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    public class MultiBranchMigrationTests
    {
        [Fact]
        public void MultiBranchMigrationCreatesRuntimeAndSyncTables()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-migrations-" + Guid.NewGuid().ToString("N"));
            var migrations = Path.Combine(root, "Migrations");
            Directory.CreateDirectory(migrations);
            var source = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Database", "Migrations"));
            foreach (var file in Directory.GetFiles(source, "*.sql"))
                File.Copy(file, Path.Combine(migrations, Path.GetFileName(file)));

            try
            {
                var dbPath = Path.Combine(root, "test.db");
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    MigrationRunner.ApplyPending(connection, migrations);

                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM BranchRuntimeConfig"));
                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='PrintQueue'"));
                    Assert.True(connection.Query("PRAGMA table_info('Invoice')").Any(x => (string)x.name == "branchId"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_invoice_branch_context'"));

                    connection.Execute(@"INSERT INTO Product (id,name,productType,serviceCost,buyPrice,stockQuantity,notes,allowDiscount,lowStockThreshold,isHiddenFromPOS,categoryId,brandId,createdAt,updatedAt)
                        VALUES ('product-test','Test', 'product',0,1,2,NULL,1,0,0,NULL,NULL,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)");
                    var payload = connection.ExecuteScalar<string>("SELECT payload FROM SyncChangeLog WHERE entityType='Product' AND entityId='product-test' ORDER BY version DESC LIMIT 1");
                    Assert.Contains("Test", payload);
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (Directory.Exists(root))
                {
                    try { Directory.Delete(root, true); }
                    catch (IOException) { }
                }
            }
        }
    }
}
