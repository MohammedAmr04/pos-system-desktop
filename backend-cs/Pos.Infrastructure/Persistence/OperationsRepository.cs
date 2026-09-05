using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Persistence
{
    public class OperationsRepository : IOperationsRepository
    {
        public PagedResult<AuditLogEntry> GetAudit(string action, string entity, int page, int size)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var parameters = new DynamicParameters(); var where = " WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(action)) { where += " AND action=@action"; parameters.Add("action", action); }
                if (!string.IsNullOrWhiteSpace(entity)) { where += " AND entityType=@entity"; parameters.Add("entity", entity); }
                var total = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM AuditLog" + where, parameters);
                parameters.Add("size", size); parameters.Add("offset", (page - 1) * size);
                return new PagedResult<AuditLogEntry> { Items = connection.Query<AuditLogEntry>("SELECT id,actorUserId,action,entityType,entityId,summary,createdAt FROM AuditLog" + where + " ORDER BY createdAt DESC LIMIT @size OFFSET @offset", parameters).ToList(), Total = total };
            }
        }

        public PagedResult<AlertItem> GetAlerts(string status, int page, int size)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var parameters = new { status = status ?? "open", size, offset = (page - 1) * size };
                return new PagedResult<AlertItem> { Items = connection.Query<AlertItem>("SELECT * FROM Alert WHERE status=@status ORDER BY createdAt DESC LIMIT @size OFFSET @offset", parameters).ToList(), Total = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Alert WHERE status=@status", parameters) };
            }
        }

        public AlertItem Acknowledge(string id, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                connection.Execute("UPDATE Alert SET status='acknowledged',acknowledgedBy=@user,acknowledgedAt=@at WHERE id=@id AND status='open'", new { id, user, at = Now() });
                var result = connection.QueryFirstOrDefault<AlertItem>("SELECT * FROM Alert WHERE id=@id", new { id });
                if (result == null) throw new NotFoundException("Alert not found");
                return result;
            }
        }

        public PagedResult<InventoryAdjustmentSummary> GetAdjustments(int page, int size)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var total = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM InventoryAdjustment");
                const string sql = @"SELECT a.id,a.number,a.reason,a.notes,a.createdBy,a.createdAt,a.status,
                    COUNT(l.id) AS lineCount,
                    SUM(CASE WHEN ABS(COALESCE(l.differenceQuantity, 0)) > 0.000001 THEN 1 ELSE 0 END) AS differenceCount
                    FROM InventoryAdjustment a LEFT JOIN InventoryAdjustmentLine l ON l.adjustmentId=a.id
                    GROUP BY a.id ORDER BY a.createdAt DESC,a.number DESC LIMIT @size OFFSET @offset";
                return new PagedResult<InventoryAdjustmentSummary> { Items = connection.Query<InventoryAdjustmentSummary>(sql, new { size, offset = (page - 1) * size }).ToList(), Total = total };
            }
        }

        public InventoryAdjustmentResult CreateAdjustment(CreateInventoryAdjustmentRequest request, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var id = Guid.NewGuid().ToString("N"); var number = connection.ExecuteScalar<int>("SELECT COALESCE(MAX(number),0)+1 FROM InventoryAdjustment", transaction: transaction);
                connection.Execute("INSERT INTO InventoryAdjustment (id,number,reason,createdBy,createdAt,status) VALUES (@id,@number,@reason,@user,@createdAt,'draft')", new { id, number, reason = request.Reason.Trim(), user, createdAt = Now() }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.created", id, "Inventory count #" + number);
                transaction.Commit(); return new InventoryAdjustmentResult { Id = id, Number = number, Status = "draft" };
            }
        }

        public InventoryAdjustmentDetail GetAdjustment(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) return GetAdjustmentCore(connection, id);
        }

        public InventoryAdjustmentDetail UpdateAdjustmentNotes(string id, UpdateInventoryAdjustmentNotesRequest request, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = GetEditableAdjustment(connection, transaction, id);
                var notes = request.Notes == null ? string.Empty : request.Notes.Trim();
                connection.Execute("UPDATE InventoryAdjustment SET notes=@notes WHERE id=@id", new { id, notes }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.notes_updated", id, "Updated notes for inventory count #" + adjustment.Number);
                transaction.Commit();
                return GetAdjustmentCore(connection, id);
            }
        }

        public InventoryAdjustmentLine UpsertAdjustmentLine(string adjustmentId, InventoryAdjustmentLineRequest request, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = GetEditableAdjustment(connection, transaction, adjustmentId);
                var product = connection.QueryFirstOrDefault<ProductRow>("SELECT id AS Id,name AS Name,stockQuantity AS StockQuantity,buyPrice AS BuyPrice FROM Product WHERE id=@id", new { id = request.ProductId }, transaction);
                if (product == null) throw new NotFoundException("Product not found");
                var unit = connection.QueryFirstOrDefault<UnitRow>("SELECT id AS Id,unitName AS UnitName,quantityFactor AS QuantityFactor,retailPrice AS RetailPrice,wholesalePrice AS WholesalePrice FROM ProductUnit WHERE id=@id AND productId=@productId", new { id = request.ProductUnitId, productId = request.ProductId }, transaction);
                if (unit == null || unit.QuantityFactor <= 0) throw new DomainValidationException("Selected unit does not belong to this product");
                var existing = connection.QueryFirstOrDefault<LineRow>("SELECT id AS Id,systemQuantity AS SystemQuantity FROM InventoryAdjustmentLine WHERE adjustmentId=@adjustmentId AND productId=@productId", new { adjustmentId, productId = request.ProductId }, transaction);
                var systemQuantity = existing == null ? product.StockQuantity : existing.SystemQuantity;
                var countedQuantity = request.IsMatched ? systemQuantity / unit.QuantityFactor : request.CountedQuantity;
                var countedBase = Math.Round(countedQuantity * unit.QuantityFactor, 6);
                var difference = Math.Round(countedBase - systemQuantity, 6);
                var unitCost = request.UnitCost ?? ResolveLastCost(connection, transaction, request.ProductId, product.BuyPrice);
                var now = Now();
                if (existing == null)
                {
                    var lineId = Guid.NewGuid().ToString("N");
                    connection.Execute(@"INSERT INTO InventoryAdjustmentLine (id,adjustmentId,productId,systemQuantity,actualQuantity,differenceQuantity,unitCost,productUnitId,unitName,quantityFactor,countedQuantity,countedBaseQuantity,isMatched,updatedAt)
                        VALUES (@id,@adjustmentId,@productId,@systemQuantity,@actualQuantity,@differenceQuantity,@unitCost,@productUnitId,@unitName,@quantityFactor,@countedQuantity,@countedBaseQuantity,@isMatched,@updatedAt)",
                        new { id = lineId, adjustmentId, productId = request.ProductId, systemQuantity, actualQuantity = countedBase, differenceQuantity = difference, unitCost, productUnitId = unit.Id, unitName = unit.UnitName, quantityFactor = unit.QuantityFactor, countedQuantity, countedBaseQuantity = countedBase, isMatched = request.IsMatched ? 1 : 0, updatedAt = now }, transaction);
                }
                else
                {
                    connection.Execute(@"UPDATE InventoryAdjustmentLine SET productUnitId=@productUnitId,unitName=@unitName,quantityFactor=@quantityFactor,countedQuantity=@countedQuantity,countedBaseQuantity=@countedBaseQuantity,actualQuantity=@actualQuantity,differenceQuantity=@differenceQuantity,unitCost=@unitCost,isMatched=@isMatched,updatedAt=@updatedAt WHERE id=@id",
                        new { id = existing.Id, productUnitId = unit.Id, unitName = unit.UnitName, quantityFactor = unit.QuantityFactor, countedQuantity, countedBaseQuantity = countedBase, actualQuantity = countedBase, differenceQuantity = difference, unitCost, isMatched = request.IsMatched ? 1 : 0, updatedAt = now }, transaction);
                }
                if (adjustment.Status == "draft") connection.Execute("UPDATE InventoryAdjustment SET status='counting' WHERE id=@id", new { id = adjustmentId }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.line_saved", adjustmentId, "Updated product in inventory count #" + adjustment.Number);
                transaction.Commit();
                return GetLine(connection, adjustmentId, request.ProductId);
            }
        }

        public void DeleteAdjustmentLine(string adjustmentId, string lineId, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = GetEditableAdjustment(connection, transaction, adjustmentId);
                var deleted = connection.Execute("DELETE FROM InventoryAdjustmentLine WHERE id=@lineId AND adjustmentId=@adjustmentId", new { lineId, adjustmentId }, transaction);
                if (deleted == 0) throw new NotFoundException("Inventory count line not found");
                WriteAudit(connection, transaction, user, "inventory.adjustment.line_deleted", adjustmentId, "Removed product from inventory count #" + adjustment.Number);
                transaction.Commit();
            }
        }

        public InventoryAdjustmentDetail PostAdjustment(string adjustmentId, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = GetEditableAdjustment(connection, transaction, adjustmentId);
                var lines = connection.Query<LineRow>("SELECT id AS Id,productId AS ProductId,systemQuantity AS SystemQuantity,countedBaseQuantity AS CountedBaseQuantity,differenceQuantity AS DifferenceQuantity,unitCost AS UnitCost FROM InventoryAdjustmentLine WHERE adjustmentId=@adjustmentId", new { adjustmentId }, transaction).ToList();
                if (lines.Count == 0) throw new DomainValidationException("Add at least one product before posting the inventory count");
                foreach (var line in lines)
                {
                    var currentStock = connection.ExecuteScalar<double?>("SELECT stockQuantity FROM Product WHERE id=@id", new { id = line.ProductId }, transaction);
                    if (!currentStock.HasValue) throw new NotFoundException("Product not found");
                    if (Math.Abs(currentStock.Value - line.SystemQuantity) > 0.000001) throw new DomainValidationException("Stock changed after this product was counted. Refresh and recount it before posting.");
                    var difference = Math.Round(line.CountedBaseQuantity - currentStock.Value, 6);
                    if (Math.Abs(difference) <= 0.000001) continue;
                    if (difference > 0)
                    {
                        if (line.UnitCost <= 0) throw new DomainValidationException("A unit cost is required for inventory increases");
                        StockLedger.Apply(connection, transaction, line.ProductId, difference, StockLedger.AdjustmentIn, adjustmentId, adjustment.Number.ToString(), false);
                        connection.Execute("INSERT INTO CostLayer (id,productId,sourcePurchaseId,quantityReceived,quantityRemaining,unitCost,createdAt) VALUES (@id,@productId,@sourceId,@quantity,@quantity,@unitCost,@createdAt)", new { id = Guid.NewGuid().ToString("N"), productId = line.ProductId, sourceId = "adjustment:" + adjustmentId, quantity = difference, unitCost = line.UnitCost, createdAt = Now() }, transaction);
                    }
                    else
                    {
                        StockLedger.Apply(connection, transaction, line.ProductId, difference, StockLedger.AdjustmentOut, adjustmentId, adjustment.Number.ToString(), true);
                        ConsumeFifo(connection, transaction, line.ProductId, -difference);
                    }
                    connection.Execute("UPDATE InventoryAdjustmentLine SET differenceQuantity=@difference WHERE id=@id", new { id = line.Id, difference }, transaction);
                    CreateLowStockAlertIfNeeded(connection, transaction, line.ProductId);
                }
                connection.Execute("UPDATE InventoryAdjustment SET status='posted',postedAt=@at,postedBy=@user WHERE id=@id", new { id = adjustmentId, at = Now(), user }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.posted", adjustmentId, "Posted inventory count #" + adjustment.Number);
                transaction.Commit(); return GetAdjustmentCore(connection, adjustmentId);
            }
        }

        public InventoryAdjustmentDetail CancelAdjustment(string adjustmentId, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = GetEditableAdjustment(connection, transaction, adjustmentId);
                connection.Execute("UPDATE InventoryAdjustment SET status='cancelled',cancelledAt=@at,cancelledBy=@user WHERE id=@id", new { id = adjustmentId, at = Now(), user }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.cancelled", adjustmentId, "Cancelled inventory count #" + adjustment.Number);
                transaction.Commit(); return GetAdjustmentCore(connection, adjustmentId);
            }
        }

        public void DeleteAdjustment(string adjustmentId, string user)
        {
            using (var connection = DbConnectionFactory.CreateConnection()) using (var transaction = connection.BeginTransaction())
            {
                var adjustment = connection.QueryFirstOrDefault<AdjustmentRow>("SELECT id AS Id,number AS Number,status AS Status FROM InventoryAdjustment WHERE id=@id", new { id = adjustmentId }, transaction);
                if (adjustment == null) throw new NotFoundException("Inventory count not found");
                if (adjustment.Status == "posted") throw new DomainValidationException("A posted inventory count cannot be deleted");
                connection.Execute("DELETE FROM InventoryAdjustmentLine WHERE adjustmentId=@adjustmentId", new { adjustmentId }, transaction);
                connection.Execute("DELETE FROM InventoryAdjustment WHERE id=@id", new { id = adjustmentId }, transaction);
                WriteAudit(connection, transaction, user, "inventory.adjustment.deleted", adjustmentId, "Deleted inventory count #" + adjustment.Number);
                transaction.Commit();
            }
        }

        private static InventoryAdjustmentDetail GetAdjustmentCore(SqliteConnection connection, string id)
        {
            var detail = connection.QueryFirstOrDefault<InventoryAdjustmentDetail>("SELECT id,number,reason,notes,createdBy,createdAt,status,postedAt,cancelledAt FROM InventoryAdjustment WHERE id=@id", new { id });
            if (detail == null) throw new NotFoundException("Inventory count not found");
            detail.Lines = connection.Query<InventoryAdjustmentLine>(@"SELECT l.id,l.productId,l.productUnitId,p.name AS productName,
                (SELECT b.barcode FROM ProductBarcode b WHERE b.productUnitId=l.productUnitId ORDER BY b.isDefault DESC LIMIT 1) AS barcode,
                l.unitName,l.quantityFactor,l.systemQuantity,l.countedQuantity,l.countedBaseQuantity,l.differenceQuantity,l.unitCost,l.isMatched,l.updatedAt,
                p.buyPrice,u.retailPrice,u.wholesalePrice FROM InventoryAdjustmentLine l JOIN Product p ON p.id=l.productId
                JOIN ProductUnit u ON u.id=l.productUnitId WHERE l.adjustmentId=@id ORDER BY l.updatedAt DESC", new { id }).ToList();
            detail.LineCount = detail.Lines.Count; detail.DifferenceCount = detail.Lines.Count(line => Math.Abs(line.DifferenceQuantity) > 0.000001); return detail;
        }

        private static InventoryAdjustmentLine GetLine(SqliteConnection connection, string adjustmentId, string productId)
        {
            return connection.QueryFirst<InventoryAdjustmentLine>(@"SELECT l.id,l.productId,l.productUnitId,p.name AS productName,
                (SELECT b.barcode FROM ProductBarcode b WHERE b.productUnitId=l.productUnitId ORDER BY b.isDefault DESC LIMIT 1) AS barcode,
                l.unitName,l.quantityFactor,l.systemQuantity,l.countedQuantity,l.countedBaseQuantity,l.differenceQuantity,l.unitCost,l.isMatched,l.updatedAt,
                p.buyPrice,u.retailPrice,u.wholesalePrice FROM InventoryAdjustmentLine l JOIN Product p ON p.id=l.productId JOIN ProductUnit u ON u.id=l.productUnitId
                WHERE l.adjustmentId=@adjustmentId AND l.productId=@productId", new { adjustmentId, productId });
        }

        private static AdjustmentRow GetEditableAdjustment(SqliteConnection connection, SqliteTransaction transaction, string id)
        {
            var adjustment = connection.QueryFirstOrDefault<AdjustmentRow>("SELECT id AS Id,number AS Number,status AS Status FROM InventoryAdjustment WHERE id=@id", new { id }, transaction);
            if (adjustment == null) throw new NotFoundException("Inventory count not found");
            if (adjustment.Status == "posted" || adjustment.Status == "cancelled") throw new DomainValidationException("A posted or cancelled inventory count cannot be changed");
            return adjustment;
        }

        private static double ResolveLastCost(SqliteConnection connection, SqliteTransaction transaction, string productId, double fallback)
        {
            return connection.ExecuteScalar<double?>("SELECT unitCost FROM CostLayer WHERE productId=@productId ORDER BY createdAt DESC,rowid DESC LIMIT 1", new { productId }, transaction) ?? fallback;
        }

        private static void ConsumeFifo(SqliteConnection connection, SqliteTransaction transaction, string productId, double quantity)
        {
            var layers = connection.Query<LayerRow>("SELECT id AS Id,quantityRemaining AS Remaining FROM CostLayer WHERE productId=@productId AND quantityRemaining > 0 ORDER BY createdAt,rowid", new { productId }, transaction).ToList();
            var remaining = quantity;
            foreach (var layer in layers)
            {
                if (remaining <= 0) break;
                var consumed = Math.Min(remaining, layer.Remaining);
                connection.Execute("UPDATE CostLayer SET quantityRemaining=quantityRemaining-@quantity WHERE id=@id", new { quantity = consumed, id = layer.Id }, transaction);
                remaining -= consumed;
            }
        }

        private static void CreateLowStockAlertIfNeeded(SqliteConnection connection, SqliteTransaction transaction, string productId)
        {
            var product = connection.QueryFirstOrDefault<ProductRow>("SELECT id AS Id,name AS Name,stockQuantity AS StockQuantity,lowStockThreshold AS Threshold FROM Product WHERE id=@id", new { id = productId }, transaction);
            if (product == null || product.Threshold <= 0 || product.StockQuantity > product.Threshold) return;
            connection.Execute("INSERT OR IGNORE INTO Alert (id,type,severity,entityType,entityId,message,status,createdAt) VALUES (@id,'low_stock','warning','product',@productId,@message,'open',@createdAt)", new { id = Guid.NewGuid().ToString("N"), productId, message = "Low stock: " + product.Name, createdAt = Now() }, transaction);
        }

        private static void WriteAudit(SqliteConnection connection, SqliteTransaction transaction, string user, string action, string entityId, string summary)
        {
            connection.Execute("INSERT INTO AuditLog (id,actorUserId,action,entityType,entityId,summary,createdAt) VALUES (@id,@user,@action,'inventory_adjustment',@entityId,@summary,@createdAt)", new { id = Guid.NewGuid().ToString("N"), user, action, entityId, summary, createdAt = Now() }, transaction);
        }

        private static string Now() { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }
        private class AdjustmentRow { public string Id { get; set; } public int Number { get; set; } public string Status { get; set; } }
        private class ProductRow { public string Id { get; set; } public string Name { get; set; } public double StockQuantity { get; set; } public double BuyPrice { get; set; } public double Threshold { get; set; } }
        private class UnitRow { public string Id { get; set; } public string UnitName { get; set; } public double QuantityFactor { get; set; } public double RetailPrice { get; set; } public double? WholesalePrice { get; set; } }
        private class LineRow { public string Id { get; set; } public string ProductId { get; set; } public double SystemQuantity { get; set; } public double CountedBaseQuantity { get; set; } public double DifferenceQuantity { get; set; } public double UnitCost { get; set; } }
        private class LayerRow { public string Id { get; set; } public double Remaining { get; set; } }
    }
}
