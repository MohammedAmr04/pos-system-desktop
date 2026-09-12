using PosCs.Application.Models;

namespace PosCs.Application.Ports
{
    public interface IOperationsRepository
    {
        PagedResult<AuditLogEntry> GetAudit(string action, string entityType, int page, int pageSize);
        PagedResult<AlertItem> GetAlerts(string status, int page, int pageSize);
        AlertItem Acknowledge(string id, string userId);
        PagedResult<InventoryAdjustmentSummary> GetAdjustments(int page, int pageSize);
        InventoryAdjustmentResult CreateAdjustment(CreateInventoryAdjustmentRequest request, string userId);
        InventoryAdjustmentDetail GetAdjustment(string id);
        InventoryAdjustmentDetail UpdateAdjustmentNotes(string id, UpdateInventoryAdjustmentNotesRequest request, string userId);
        InventoryAdjustmentLine UpsertAdjustmentLine(string adjustmentId, InventoryAdjustmentLineRequest request, string userId);
        void DeleteAdjustmentLine(string adjustmentId, string lineId, string userId);
        InventoryAdjustmentDetail PostAdjustment(string adjustmentId, string userId);
        InventoryAdjustmentDetail CancelAdjustment(string adjustmentId, string userId);
        void DeleteAdjustment(string adjustmentId, string userId);
    }
}
