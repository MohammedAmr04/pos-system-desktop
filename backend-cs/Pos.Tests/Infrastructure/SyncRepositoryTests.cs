using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using PosCs.Application.Models;
using PosCs.Infrastructure.Data;
using PosCs.Infrastructure.Persistence;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    [Collection("DbConnectionFactory")]
    public class SyncRepositoryTests
    {
        [Fact]
        public void ApplyReceivedSaleReturnUpdatesInvoiceReturnStatus()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    connection.Execute("INSERT INTO Invoice (id,totalAmount) VALUES ('inv-1',100)");
                }

                var repository = new SyncRepository();
                var operation = BuildSaleReturnOperation("op-1", "inv-1", "partial");
                repository.Receive(operation);
                var outcome = repository.ApplyReceived(operation.Id);

                Assert.True(outcome.Applied);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal("partial", connection.ExecuteScalar<string>("SELECT returnStatus FROM Invoice WHERE id='inv-1'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SaleReturn WHERE id='ret-1'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedSaleReturnFailsWhenInvoiceHasNotSynced()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                var repository = new SyncRepository();
                var operation = BuildSaleReturnOperation("op-2", "missing-invoice", "partial");
                repository.Receive(operation);

                var outcome = repository.ApplyReceived(operation.Id);

                Assert.False(outcome.Applied);
                Assert.False(string.IsNullOrWhiteSpace(outcome.Error));
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SaleReturn"));
                    Assert.Equal("failed", connection.ExecuteScalar<string>("SELECT status FROM SyncInbox WHERE operationId='op-2'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void FailedOperationBacksOffThenMovesToDeadAfterMaxAttempts()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    InsertOutbox(connection, "op-a", "pending");
                }

                var repository = new SyncRepository();
                var pending = repository.GetPending(10);
                Assert.Single(pending);
                Assert.Equal("op-a", pending[0].Id);

                repository.MarkAttempt("op-a", "boom");

                Assert.Empty(repository.GetPending(10));
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal("failed", connection.ExecuteScalar<string>("SELECT status FROM SyncOutbox WHERE id='op-a'"));
                    Assert.NotNull(connection.ExecuteScalar<DateTime?>("SELECT nextAttemptAt FROM SyncOutbox WHERE id='op-a'"));
                }

                for (var i = 2; i <= 8; i++)
                    repository.MarkAttempt("op-a", "boom");

                Assert.Empty(repository.GetPending(10));
                Assert.Equal(1, repository.GetStatus().Dead);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal("dead", connection.ExecuteScalar<string>("SELECT status FROM SyncOutbox WHERE id='op-a'"));
                    Assert.Equal(8, connection.ExecuteScalar<int>("SELECT attempts FROM SyncOutbox WHERE id='op-a'"));
                    Assert.Null(connection.ExecuteScalar<DateTime?>("SELECT nextAttemptAt FROM SyncOutbox WHERE id='op-a'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void DueFailedOperationIsReturnedByGetPending()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    InsertOutbox(connection, "op-b", "pending");
                }

                var repository = new SyncRepository();
                repository.MarkAttempt("op-b", "boom");
                Assert.Empty(repository.GetPending(10));

                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    connection.Execute("UPDATE SyncOutbox SET nextAttemptAt=@past WHERE id='op-b'", new { past = DateTime.Now.AddMinutes(-1) });
                }

                var pending = repository.GetPending(10);
                Assert.Single(pending);
                Assert.Equal("op-b", pending[0].Id);
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedPurchaseInvoiceCreatesHeaderAndLines()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                var repository = new SyncRepository();
                var operation = BuildPurchaseInvoiceOperation("op-pi-1", "pur-1", "posted", "pi-1");
                repository.Receive(operation);

                var outcome = repository.ApplyReceived(operation.Id);

                Assert.True(outcome.Applied);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal("posted", connection.ExecuteScalar<string>("SELECT status FROM PurchaseInvoice WHERE id='pur-1'"));
                    Assert.Equal(20.0, connection.ExecuteScalar<double>("SELECT total FROM PurchaseInvoice WHERE id='pur-1'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseInvoiceItem WHERE purchaseInvoiceId='pur-1'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedPurchaseInvoiceUpsertReflectsCancellation()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                var repository = new SyncRepository();
                var posted = BuildPurchaseInvoiceOperation("op-pi-2", "pur-2", "posted", "pi-2");
                repository.Receive(posted);
                Assert.True(repository.ApplyReceived(posted.Id).Applied);

                var cancelled = BuildPurchaseInvoiceOperation("op-pi-3", "pur-2", "cancelled", null);
                repository.Receive(cancelled);
                Assert.True(repository.ApplyReceived(cancelled.Id).Applied);

                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal("cancelled", connection.ExecuteScalar<string>("SELECT status FROM PurchaseInvoice WHERE id='pur-2'"));
                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseInvoiceItem WHERE purchaseInvoiceId='pur-2'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedPurchaseReturnFailsWhenPurchaseHasNotSynced()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                var repository = new SyncRepository();
                var operation = BuildPurchaseReturnOperation("op-pr-1", "pret-1", "missing-purchase", 1);
                repository.Receive(operation);

                var outcome = repository.ApplyReceived(operation.Id);

                Assert.False(outcome.Applied);
                Assert.False(string.IsNullOrWhiteSpace(outcome.Error));
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseReturn"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedPurchaseReturnAppliesAndUpdatesReturnStatus()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    var now = DateTime.Now;
                    connection.Execute("INSERT INTO PurchaseInvoice (id,invoiceNumber,date,paymentMethod,status,subtotal,discount,tax,total,createdAt,updatedAt) " +
                        "VALUES ('pur-3',3,@now,'cash','posted',20,0,0,20,@now,@now)", new { now });
                    connection.Execute("INSERT INTO PurchaseInvoiceItem (id,purchaseInvoiceId,productId,productUnitId,unitName,quantityFactor,quantity,unitCost,lineTotal) " +
                        "VALUES ('pi-3','pur-3','prod-1','unit-1','Piece',1,2,10,20)");
                }

                var repository = new SyncRepository();
                var operation = BuildPurchaseReturnOperation("op-pr-2", "pret-2", "pur-3", 2);
                repository.Receive(operation);

                var outcome = repository.ApplyReceived(operation.Id);

                Assert.True(outcome.Applied);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseReturn WHERE id='pret-2'"));
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PurchaseReturnDetail WHERE returnId='pret-2'"));
                    Assert.Equal("full", connection.ExecuteScalar<string>("SELECT returnStatus FROM PurchaseInvoice WHERE id='pur-3'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void ApplyReceivedInventoryAdjustmentCreatesHeaderAndLines()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                var repository = new SyncRepository();
                var operation = BuildAdjustmentOperation("op-adj-1", "adj-1");
                repository.Receive(operation);

                var outcome = repository.ApplyReceived(operation.Id);

                Assert.True(outcome.Applied);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    var row = connection.QueryFirstOrDefault("SELECT branchId,status FROM InventoryAdjustment WHERE id='adj-1'");
                    Assert.NotNull(row);
                    Assert.Equal("posted", (string)row.status);
                    Assert.Equal("default", (string)row.branchId);
                    Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(1) FROM InventoryAdjustmentLine WHERE adjustmentId='adj-1'"));
                    Assert.Equal(3.0, connection.ExecuteScalar<double>("SELECT actualQuantity FROM InventoryAdjustmentLine WHERE adjustmentId='adj-1'"));
                }
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        private static void InsertOutbox(SqliteConnection connection, string id, string status)
        {
            connection.Execute(
                "INSERT INTO SyncOutbox (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status) " +
                "VALUES (@id,'default','default-terminal','upsert','Invoice','inv-1',@at,@status)",
                new { id, at = DateTime.Now, status });
        }

        private static SyncOperation BuildSaleReturnOperation(string operationId, string invoiceId, string invoiceReturnStatus)
        {
            var saleReturn = new PosCs.Domain.Entities.SaleReturn
            {
                Id = "ret-1",
                Number = 1,
                InvoiceId = invoiceId,
                Date = DateTime.Now,
                TotalAmount = 10,
                RestoredCost = 5,
                PaymentMethod = "cash",
                Status = "posted",
                CreatedAt = DateTime.Now
            };
            var details = new List<PosCs.Domain.Entities.SaleReturnDetail>
            {
                new PosCs.Domain.Entities.SaleReturnDetail
                {
                    Id = "det-1",
                    InvoiceDetailId = "line-1",
                    ProductId = "prod-1",
                    Quantity = 1,
                    QuantityFactor = 1,
                    UnitPrice = 10,
                    LineTotal = 10,
                    RestoredCost = 5
                }
            };

            return new SyncOperation
            {
                Id = operationId,
                BranchId = "default",
                TerminalId = "default-terminal",
                OperationType = "upsert",
                EntityType = "SaleReturn",
                EntityId = saleReturn.Id,
                Payload = JsonConvert.SerializeObject(new { saleReturn, details, invoiceReturnStatus })
            };
        }

        private static SyncOperation BuildPurchaseInvoiceOperation(string operationId, string purchaseId, string status, string itemId)
        {
            var purchaseInvoice = new PosCs.Domain.Entities.PurchaseInvoice
            {
                Id = purchaseId,
                InvoiceNumber = 1,
                SupplierInvoiceNumber = "S-1",
                SupplierId = "sup-1",
                Date = DateTime.Now,
                PaymentMethod = "cash",
                Status = status,
                Subtotal = 20,
                Discount = 0,
                Tax = 0,
                Total = 20,
                CreatedBy = "admin",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var items = new List<PosCs.Domain.Entities.PurchaseInvoiceItem>();
            if (itemId != null)
            {
                items.Add(new PosCs.Domain.Entities.PurchaseInvoiceItem
                {
                    Id = itemId,
                    PurchaseInvoiceId = purchaseId,
                    ProductId = "prod-1",
                    ProductUnitId = "unit-1",
                    UnitName = "Piece",
                    QuantityFactor = 1,
                    Quantity = 2,
                    UnitCost = 10,
                    LineTotal = 20
                });
            }

            return new SyncOperation
            {
                Id = operationId,
                BranchId = "default",
                TerminalId = "default-terminal",
                OperationType = "upsert",
                EntityType = "PurchaseInvoice",
                EntityId = purchaseId,
                Payload = JsonConvert.SerializeObject(new { purchaseInvoice, items })
            };
        }

        private static SyncOperation BuildPurchaseReturnOperation(string operationId, string returnId, string purchaseId, double quantity)
        {
            var purchaseReturn = new PosCs.Domain.Entities.PurchaseReturn
            {
                Id = returnId,
                Number = 1,
                PurchaseInvoiceId = purchaseId,
                Date = DateTime.Now,
                TotalAmount = quantity * 10,
                PaymentMethod = "cash",
                Status = "posted",
                CreatedBy = "admin",
                CreatedAt = DateTime.Now
            };
            var details = new List<PosCs.Domain.Entities.PurchaseReturnDetail>
            {
                new PosCs.Domain.Entities.PurchaseReturnDetail
                {
                    Id = "prd-" + returnId,
                    ReturnId = returnId,
                    PurchaseItemId = "pi-3",
                    ProductId = "prod-1",
                    UnitName = "Piece",
                    Quantity = quantity,
                    QuantityFactor = 1,
                    UnitCost = 10,
                    LineTotal = quantity * 10
                }
            };

            return new SyncOperation
            {
                Id = operationId,
                BranchId = "default",
                TerminalId = "default-terminal",
                OperationType = "upsert",
                EntityType = "PurchaseReturn",
                EntityId = returnId,
                Payload = JsonConvert.SerializeObject(new { purchaseReturn, details })
            };
        }

        private static SyncOperation BuildAdjustmentOperation(string operationId, string adjustmentId)
        {
            var detail = new InventoryAdjustmentDetail
            {
                Id = adjustmentId,
                Number = 1,
                Reason = "count",
                CreatedBy = "admin",
                CreatedAt = DateTime.Now,
                Status = "posted",
                PostedAt = DateTime.Now,
                Lines = new List<InventoryAdjustmentLine>
                {
                    new InventoryAdjustmentLine
                    {
                        Id = "adjl-" + adjustmentId,
                        ProductId = "prod-1",
                        ProductUnitId = "unit-1",
                        UnitName = "Piece",
                        QuantityFactor = 1,
                        SystemQuantity = 5,
                        CountedQuantity = 3,
                        CountedBaseQuantity = 3,
                        DifferenceQuantity = -2,
                        UnitCost = 10,
                        IsMatched = false,
                        UpdatedAt = DateTime.Now
                    }
                }
            };

            return new SyncOperation
            {
                Id = operationId,
                BranchId = "default",
                TerminalId = "default-terminal",
                OperationType = "upsert",
                EntityType = "InventoryAdjustment",
                EntityId = adjustmentId,
                Payload = JsonConvert.SerializeObject(detail)
            };
        }

        private static string CreateMigratedDatabase()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-syncrepo-" + Guid.NewGuid().ToString("N"));
            var migrations = Path.Combine(root, "Migrations");
            Directory.CreateDirectory(migrations);
            var source = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Database", "Migrations"));
            foreach (var file in Directory.GetFiles(source, "*.sql"))
                File.Copy(file, Path.Combine(migrations, Path.GetFileName(file)));

            var dbPath = Path.Combine(root, "test.db");
            using (var connection = new SqliteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                MigrationRunner.ApplyPending(connection, migrations);
            }
            return dbPath;
        }

        private static void Cleanup(string dbPath)
        {
            SqliteConnection.ClearAllPools();
            var root = Path.GetDirectoryName(dbPath);
            if (root != null && Directory.Exists(root))
            {
                try { Directory.Delete(root, true); }
                catch (IOException) { }
            }
        }
    }
}
