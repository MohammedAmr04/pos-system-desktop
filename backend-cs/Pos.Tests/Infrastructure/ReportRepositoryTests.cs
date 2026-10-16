using System;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Infrastructure.Data;
using PosCs.Infrastructure.Persistence;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    [Collection("DbConnectionFactory")]
    public class ReportRepositoryTests
    {
        [Fact]
        public void BranchStockAggregatesLedgerPerBranch()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    InsertProduct(connection, "p1", "Widget");
                    InsertMovement(connection, "m1", "p1", 10, "branch-a");
                    InsertMovement(connection, "m2", "p1", -3, "branch-a");
                    InsertMovement(connection, "m3", "p1", 4, "branch-b");
                }

                var repository = new ReportRepository();

                var branchA = repository.GetBranchStock("branch-a");
                Assert.Single(branchA);
                Assert.Equal(7.0, branchA[0].Quantity);
                Assert.Equal("branch-a", branchA[0].BranchId);
                Assert.Equal("Widget", branchA[0].ProductName);

                var branchB = repository.GetBranchStock("branch-b");
                Assert.Single(branchB);
                Assert.Equal(4.0, branchB[0].Quantity);
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        [Fact]
        public void BranchStockExcludesZeroNetProducts()
        {
            var dbPath = CreateMigratedDatabase();
            try
            {
                DbConnectionFactory.UseDatabase(dbPath);
                using (var connection = new SqliteConnection("Data Source=" + dbPath))
                {
                    connection.Open();
                    InsertProduct(connection, "p1", "Widget");
                    InsertMovement(connection, "m1", "p1", 5, "branch-a");
                    InsertMovement(connection, "m2", "p1", -5, "branch-a");
                }

                var rows = new ReportRepository().GetBranchStock("branch-a");

                Assert.Empty(rows);
            }
            finally
            {
                Cleanup(dbPath);
            }
        }

        private static void InsertProduct(SqliteConnection connection, string id, string name)
        {
            connection.Execute(@"INSERT INTO Product (id,name,productType,serviceCost,buyPrice,stockQuantity,notes,allowDiscount,lowStockThreshold,isHiddenFromPOS,categoryId,brandId,createdAt,updatedAt)
                VALUES (@id,@name,'product',0,1,0,NULL,1,0,0,NULL,NULL,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)", new { id, name });
        }

        private static void InsertMovement(SqliteConnection connection, string id, string productId, double quantity, string branchId)
        {
            connection.Execute("INSERT INTO StockMovement (id,productId,quantity,type,createdAt,branchId) VALUES (@id,@productId,@quantity,'sale',CURRENT_TIMESTAMP,@branchId)",
                new { id, productId, quantity, branchId });
        }

        private static string CreateMigratedDatabase()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-reportrepo-" + Guid.NewGuid().ToString("N"));
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
