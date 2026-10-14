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

        [Fact]
        public void BranchOutboxTriggersAttachPayloadsAndSkipCentralNode()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-sync-outbox-" + Guid.NewGuid().ToString("N"));
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

                    connection.Execute("INSERT INTO Payment (id,amount,paymentMethod,date,createdAt) VALUES ('pay-1',10,'cash',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)");
                    connection.Execute("INSERT INTO Shift (id,number,openedBy,openingCash,openedAt) VALUES ('shift-1',1001,'user-1',0,CURRENT_TIMESTAMP)");
                    connection.Execute("INSERT INTO Expense (id,categoryId,amount,date,createdAt) VALUES ('exp-1','exp-cat-rent',5,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)");
                    connection.Execute("INSERT INTO StockMovement (id,productId,quantity,type,createdAt) VALUES ('sm-1','product-1',-2,'sale',CURRENT_TIMESTAMP)");

                    var paymentPayload = connection.ExecuteScalar<string>("SELECT payload FROM SyncOutbox WHERE entityType='Payment' AND entityId='pay-1'");
                    Assert.Contains("\"amount\"", paymentPayload);
                    var parsedPayment = Newtonsoft.Json.JsonConvert.DeserializeObject<PosCs.Domain.Entities.Payment>(paymentPayload);
                    Assert.NotNull(parsedPayment);
                    Assert.NotEqual(default(DateTime), parsedPayment.Date);
                    var movementPayload = connection.ExecuteScalar<string>("SELECT payload FROM SyncOutbox WHERE entityType='StockMovement' AND entityId='sm-1'");
                    Assert.Contains("sale", movementPayload);
                    Assert.Equal(4, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox WHERE status='pending'"));

                    connection.Execute("UPDATE BranchRuntimeConfig SET nodeRole='central' WHERE id=1");
                    connection.Execute("DELETE FROM SyncOutbox");
                    connection.Execute("INSERT INTO Payment (id,amount,paymentMethod,date,createdAt) VALUES ('pay-2',10,'cash',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)");
                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox"));
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
