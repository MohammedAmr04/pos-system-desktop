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
