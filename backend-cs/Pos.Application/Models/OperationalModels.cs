using System;
using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public class AuditLogEntry { public string Id { get; set; } public string ActorUserId { get; set; } public string Action { get; set; } public string EntityType { get; set; } public string EntityId { get; set; } public string Summary { get; set; } public DateTime CreatedAt { get; set; } }
    public class AlertItem { public string Id { get; set; } public string Type { get; set; } public string Severity { get; set; } public string EntityType { get; set; } public string EntityId { get; set; } public string Message { get; set; } public string Status { get; set; } public string AcknowledgedBy { get; set; } public DateTime? AcknowledgedAt { get; set; } public DateTime CreatedAt { get; set; } }
    public class CreateInventoryAdjustmentRequest { public string Reason { get; set; } }
    public class UpdateInventoryAdjustmentNotesRequest { public string Notes { get; set; } }
    public class InventoryAdjustmentLineRequest { public string ProductId { get; set; } public string ProductUnitId { get; set; } public double CountedQuantity { get; set; } public double? UnitCost { get; set; } public bool IsMatched { get; set; } }
    public class InventoryAdjustmentResult { public string Id { get; set; } public int Number { get; set; } public string Status { get; set; } }
    public class InventoryAdjustmentSummary { public string Id { get; set; } public int Number { get; set; } public string Reason { get; set; } public string Notes { get; set; } public string CreatedBy { get; set; } public DateTime CreatedAt { get; set; } public string Status { get; set; } public int LineCount { get; set; } public int DifferenceCount { get; set; } }
    public class InventoryAdjustmentLine
    {
        public string Id { get; set; } public string ProductId { get; set; } public string ProductUnitId { get; set; }
        public string ProductName { get; set; } public string Barcode { get; set; } public string UnitName { get; set; }
        public double QuantityFactor { get; set; } public double SystemQuantity { get; set; } public double CountedQuantity { get; set; }
        public double CountedBaseQuantity { get; set; } public double DifferenceQuantity { get; set; } public double UnitCost { get; set; }
        public double BuyPrice { get; set; } public double RetailPrice { get; set; } public double? WholesalePrice { get; set; }
        public bool IsMatched { get; set; } public DateTime? UpdatedAt { get; set; }
    }
    public class InventoryAdjustmentDetail : InventoryAdjustmentSummary { public DateTime? PostedAt { get; set; } public DateTime? CancelledAt { get; set; } public List<InventoryAdjustmentLine> Lines { get; set; } }
    public class BackupSummary { public string FileName { get; set; } public long SizeBytes { get; set; } public DateTime CreatedAt { get; set; } }
    public class RestoreBackupRequest { public string FileName { get; set; } }
}
