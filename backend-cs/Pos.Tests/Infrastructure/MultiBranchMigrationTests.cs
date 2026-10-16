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
                    Assert.True(connection.Query("PRAGMA table_info('Branch')").Any(x => (string)x.name == "secretHash"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_invoice_branch_context'"));

                    // Migration 051: branch/terminal administration + switch audit.
                    Assert.True(connection.Query("PRAGMA table_info('BranchRuntimeConfig')").Any(x => (string)x.name == "centralToken"));
                    Assert.True(connection.Query("PRAGMA table_info('BranchRuntimeConfig')").Any(x => (string)x.name == "centralBranchKey"));
                    Assert.True(connection.Query("PRAGMA table_info('BranchRuntimeConfig')").Any(x => (string)x.name == "status"));
                    Assert.True(connection.Query("PRAGMA table_info('Terminal')").Any(x => (string)x.name == "lastSeenAt"));
                    Assert.True(connection.Query("PRAGMA table_info('Terminal')").Any(x => (string)x.name == "notes"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='BranchAssignmentHistory'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='index' AND name='idx_branch_assignment_terminal'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='index' AND name='idx_terminal_branch'"));

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

                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_sale_return_sync_insert'"));
                    using (var tx = connection.BeginTransaction())
                    {
                        PosCs.Infrastructure.Persistence.SyncOutboxWriter.Enqueue(connection, tx, "upsert", "SaleReturn", "ret-1",
                            new { saleReturn = new { id = "ret-1", totalAmount = 5.0 }, details = new[] { new { id = "d-1" } } });
                        tx.Commit();
                    }
                    var returnPayload = connection.ExecuteScalar<string>("SELECT payload FROM SyncOutbox WHERE entityType='SaleReturn' AND entityId='ret-1'");
                    Assert.Contains("\"saleReturn\"", returnPayload);
                    Assert.Contains("\"details\"", returnPayload);

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

        [Fact]
        public void DocumentNumbersAreUniquePerBranch()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-numbering-" + Guid.NewGuid().ToString("N"));
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

                    // Rebuilt triggers must survive the table rebuild (048).
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_shift_sync_insert'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_shift_branch_context'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type='trigger' AND name='trg_inventory_adjustment_branch_context'"));

                    // Same number on two different branches must both persist.
                    connection.Execute("INSERT INTO PurchaseReturn (id,number,purchaseInvoiceId,date,totalAmount,createdAt,branchId) VALUES ('pr-a',1,'p1',CURRENT_TIMESTAMP,10,CURRENT_TIMESTAMP,'branch-a')");
                    connection.Execute("INSERT INTO PurchaseReturn (id,number,purchaseInvoiceId,date,totalAmount,createdAt,branchId) VALUES ('pr-b',1,'p2',CURRENT_TIMESTAMP,10,CURRENT_TIMESTAMP,'branch-b')");
                    Assert.Equal(2, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseReturn WHERE number=1"));

                    connection.Execute("INSERT INTO Shift (id,number,openedBy,openingCash,openedAt,branchId) VALUES ('s-a',1,'u',0,CURRENT_TIMESTAMP,'branch-a')");
                    connection.Execute("INSERT INTO Shift (id,number,openedBy,openingCash,openedAt,branchId) VALUES ('s-b',1,'u',0,CURRENT_TIMESTAMP,'branch-b')");
                    Assert.Equal(2, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Shift WHERE number=1"));

                    // The same number on the same branch is still rejected.
                    Assert.Throws<SqliteException>(() =>
                        connection.Execute("INSERT INTO PurchaseReturn (id,number,purchaseInvoiceId,date,totalAmount,createdAt,branchId) VALUES ('pr-c',1,'p3',CURRENT_TIMESTAMP,10,CURRENT_TIMESTAMP,'branch-a')"));
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
