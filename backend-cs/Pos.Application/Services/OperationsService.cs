using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    public class OperationsService
    {
        private readonly IOperationsRepository _repo;
        public OperationsService(IOperationsRepository repo) { _repo = repo; }
        public PagedResult<AuditLogEntry> Audit(string action, string entity, int page, int size) { return _repo.GetAudit(action, entity, NormalizePage(page), NormalizeSize(size)); }
        public PagedResult<AlertItem> Alerts(string status, int page, int size) { return _repo.GetAlerts(status, NormalizePage(page), NormalizeSize(size)); }
        public AlertItem Acknowledge(string id, string user) { return _repo.Acknowledge(id, user); }
        public PagedResult<InventoryAdjustmentSummary> GetAdjustments(int page, int size) { return _repo.GetAdjustments(NormalizePage(page), NormalizeSize(size)); }
        public InventoryAdjustmentResult CreateAdjustment(CreateInventoryAdjustmentRequest request, string user) { if (request == null || string.IsNullOrWhiteSpace(request.Reason)) throw new DomainValidationException("Reason is required"); return _repo.CreateAdjustment(request, user); }
        public InventoryAdjustmentDetail GetAdjustment(string id) { return _repo.GetAdjustment(id); }
        public InventoryAdjustmentLine SaveLine(string id, InventoryAdjustmentLineRequest request, string user) { if (request == null || string.IsNullOrWhiteSpace(request.ProductId) || string.IsNullOrWhiteSpace(request.ProductUnitId)) throw new DomainValidationException("Product and unit are required"); if (request.CountedQuantity < 0) throw new DomainValidationException("Counted quantity cannot be negative"); if (request.UnitCost.HasValue && request.UnitCost.Value < 0) throw new DomainValidationException("Unit cost cannot be negative"); return _repo.UpsertAdjustmentLine(id, request, user); }
        public void DeleteLine(string id, string lineId, string user) { _repo.DeleteAdjustmentLine(id, lineId, user); }
        public InventoryAdjustmentDetail Post(string id, string user) { return _repo.PostAdjustment(id, user); }
        public InventoryAdjustmentDetail Cancel(string id, string user) { return _repo.CancelAdjustment(id, user); }
        private static int NormalizePage(int page) { return page < 1 ? 1 : page; }
        private static int NormalizeSize(int size) { return size < 1 ? 20 : (size > 100 ? 100 : size); }
    }
}
