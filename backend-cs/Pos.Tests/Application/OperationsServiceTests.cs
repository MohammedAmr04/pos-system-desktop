using System;
using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeOperationsRepository : IOperationsRepository
    {
        public CreateInventoryAdjustmentRequest CreateRequest;
        public string UpdatedId;
        public string UpdatedNotes;
        public InventoryAdjustmentDetail Adjustment = new InventoryAdjustmentDetail
        {
            Id = "adjustment-1",
            Number = 1,
            Reason = "Inventory count",
            Status = "draft",
            Lines = new List<InventoryAdjustmentLine>()
        };

        public PagedResult<AuditLogEntry> GetAudit(string action, string entityType, int page, int pageSize) { return new PagedResult<AuditLogEntry>(); }
        public PagedResult<AlertItem> GetAlerts(string status, int page, int pageSize) { return new PagedResult<AlertItem>(); }
        public AlertItem Acknowledge(string id, string userId) { return new AlertItem(); }
        public PagedResult<InventoryAdjustmentSummary> GetAdjustments(int page, int pageSize) { return new PagedResult<InventoryAdjustmentSummary>(); }
        public InventoryAdjustmentResult CreateAdjustment(CreateInventoryAdjustmentRequest request, string userId) { CreateRequest = request; return new InventoryAdjustmentResult { Id = "adjustment-1", Number = 1, Status = "draft" }; }
        public InventoryAdjustmentDetail GetAdjustment(string id) { return Adjustment; }
        public InventoryAdjustmentDetail UpdateAdjustmentNotes(string id, UpdateInventoryAdjustmentNotesRequest request, string userId)
        {
            UpdatedId = id;
            UpdatedNotes = request.Notes;
            Adjustment.Notes = request.Notes;
            return Adjustment;
        }
        public InventoryAdjustmentLine UpsertAdjustmentLine(string adjustmentId, InventoryAdjustmentLineRequest request, string userId) { return new InventoryAdjustmentLine(); }
        public void DeleteAdjustmentLine(string adjustmentId, string lineId, string userId) { }
        public InventoryAdjustmentDetail PostAdjustment(string adjustmentId, string userId) { return Adjustment; }
        public InventoryAdjustmentDetail CancelAdjustment(string adjustmentId, string userId) { return Adjustment; }
        public string DeletedId;
        public void DeleteAdjustment(string adjustmentId, string userId) { DeletedId = adjustmentId; }
    }

    public class OperationsServiceTests
    {
        [Fact]
        public void Create_Uses_Default_Reason_When_None_Is_Provided()
        {
            var repository = new FakeOperationsRepository();
            var service = new OperationsService(repository);

            service.CreateAdjustment(new CreateInventoryAdjustmentRequest(), "user-1");

            Assert.Equal("Inventory count", repository.CreateRequest.Reason);
        }

        [Fact]
        public void Update_Notes_Trims_And_Persists_An_Empty_Value()
        {
            var repository = new FakeOperationsRepository();
            var service = new OperationsService(repository);

            service.UpdateAdjustmentNotes("adjustment-1", new UpdateInventoryAdjustmentNotesRequest { Notes = "  " }, "user-1");

            Assert.Equal("adjustment-1", repository.UpdatedId);
            Assert.Equal(string.Empty, repository.UpdatedNotes);
        }

        [Fact]
        public void Update_Notes_Rejects_Posted_Or_Cancelled_Sessions()
        {
            var repository = new FakeOperationsRepository();
            var service = new OperationsService(repository);
            repository.Adjustment.Status = "posted";

            Assert.Throws<DomainValidationException>(() =>
                service.UpdateAdjustmentNotes("adjustment-1", new UpdateInventoryAdjustmentNotesRequest { Notes = "note" }, "user-1"));
        }

        [Fact]
        public void Delete_Adjustment_Delegates_To_Repository()
        {
            var repository = new FakeOperationsRepository();
            var service = new OperationsService(repository);

            service.DeleteAdjustment("adjustment-1", "user-1");

            Assert.Equal("adjustment-1", repository.DeletedId);
        }
    }
}
