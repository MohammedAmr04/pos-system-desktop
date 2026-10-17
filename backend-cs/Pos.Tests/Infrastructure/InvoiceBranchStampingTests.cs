using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Infrastructure.Data;
using PosCs.Infrastructure.Persistence;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    [Collection("DbConnectionFactory")]
    public class InvoiceBranchStampingTests
    {
        [Fact]
        public void InvoiceIsStampedWithScopeBranchEvenWhenSpoofed()
        {
            var root = Path.Combine(Path.GetTempPath(), "pos-stamping-" + Guid.NewGuid().ToString("N"));
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
                }

                DbConnectionFactory.UseDatabase(dbPath);

                var branches = new BranchRepository();
                branches.EnsureBranchAndTerminal("branch-x", "terminal-x");

                try
                {
                    BranchScope.Set("branch-x", "terminal-x", false);
                    var repo = new InvoiceRepository();
                    var created = repo.Create(
                        new Invoice { Status = "draft", TotalAmount = 10, CreatedBy = "u1", BranchId = "spoofed", TerminalId = "spoofed" },
                        new List<InvoiceDetail>());

                    var stored = repo.GetById(created.Id);
                    Assert.Equal("branch-x", stored.BranchId);
                    Assert.Equal("terminal-x", stored.TerminalId);
                }
                finally
                {
                    BranchScope.Clear();
                }

                var fallback = new InvoiceRepository().Create(
                    new Invoice { Status = "draft", TotalAmount = 5, CreatedBy = "u1" },
                    new List<InvoiceDetail>());
                Assert.Equal("default", new InvoiceRepository().GetById(fallback.Id).BranchId);
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
