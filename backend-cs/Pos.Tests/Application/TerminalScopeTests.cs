using System;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class TerminalScopeTests
    {
        private static InvoiceService InvoiceServiceWith(StubPagedInvoiceRepository invoices)
        {
            return new InvoiceService(invoices, new StubProductRepository(), new StubProductUnitRepository(),
                new StubAccessControl(), new StubClock(), new RecordingClientRepository(),
                new FakeEmployeeRepository(), new FakePaymentRepository(), AmbientBranchContext.Instance);
        }

        private static ReportsService ReportsServiceWith(FakeReportRepository reports)
        {
            return new ReportsService(new FakeProductsRepository(), reports, AmbientBranchContext.Instance);
        }

        [Fact]
        public void Scope_ClampsNonAdminAndKeepsAdminChoice()
        {
            try
            {
                BranchScope.Set("branch-1", "terminal-1", false);
                Assert.Equal("branch-1", AmbientBranchContext.Instance.ApplyScope("branch-2"));
                Assert.Equal("branch-1", AmbientBranchContext.Instance.ApplyScope(null));

                BranchScope.Set("branch-1", "terminal-1", true);
                Assert.Equal("branch-2", AmbientBranchContext.Instance.ApplyScope("branch-2"));
                Assert.Null(AmbientBranchContext.Instance.ApplyScope(null));
            }
            finally
            {
                BranchScope.Clear();
            }
        }

        [Fact]
        public void InvoicePaged_ClampsCashierToOwnBranch()
        {
            var repo = new StubPagedInvoiceRepository();
            try
            {
                BranchScope.Set("branch-1", "terminal-1", false);
                InvoiceServiceWith(repo).GetPaged(1, 20, null, null, null, null, null, null, "branch-2");
                Assert.Equal("branch-1", repo.LastBranchId);
            }
            finally
            {
                BranchScope.Clear();
            }
        }

        [Fact]
        public void InvoicePaged_AdminKeepsRequestedBranch()
        {
            var repo = new StubPagedInvoiceRepository();
            try
            {
                BranchScope.Set("branch-1", "terminal-1", true);
                InvoiceServiceWith(repo).GetPaged(1, 20, null, null, null, null, null, null, "branch-2");
                Assert.Equal("branch-2", repo.LastBranchId);
            }
            finally
            {
                BranchScope.Clear();
            }
        }

        [Fact]
        public void InvoiceById_ForCashierRejectsOtherBranch()
        {
            var repo = new StubPagedInvoiceRepository
            {
                Page = new System.Collections.Generic.List<Invoice>
                {
                    new Invoice { Id = "inv-1", BranchId = "branch-2" }
                }
            };
            try
            {
                BranchScope.Set("branch-1", "terminal-1", false);
                Assert.Throws<NotFoundException>(() => InvoiceServiceWith(repo).GetById("inv-1"));

                BranchScope.Set("branch-1", "terminal-1", true);
                Assert.NotNull(InvoiceServiceWith(repo).GetById("inv-1"));
            }
            finally
            {
                BranchScope.Clear();
            }
        }

        [Fact]
        public void Reports_ClampsCashierToOwnBranch()
        {
            var repo = new FakeReportRepository();
            try
            {
                BranchScope.Set("branch-1", "terminal-1", false);
                ReportsServiceWith(repo).Sales(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), "branch-2");
                Assert.Equal("branch-1", repo.LastBranchId);
            }
            finally
            {
                BranchScope.Clear();
            }
        }

        [Fact]
        public void BranchStock_ForCashierForcesOwnBranch()
        {
            var repo = new FakeReportRepository();
            try
            {
                BranchScope.Set("branch-1", "terminal-1", false);
                ReportsServiceWith(repo).BranchStock("branch-2");
                Assert.Equal("branch-1", repo.LastBranchId);
            }
            finally
            {
                BranchScope.Clear();
            }
        }
    }
}
