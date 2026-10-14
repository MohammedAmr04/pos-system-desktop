using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using Newtonsoft.Json;

namespace PosCs.Infrastructure.Persistence
{
    public class SyncRepository : ISyncRepository
    {
        private const int MaxAttempts = 8;
        private const int BaseBackoffSeconds = 10;
        private const int MaxBackoffSeconds = 1800;

        public List<SyncOperation> GetPending(int limit)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                return connection.Query<SyncOperation>(@"SELECT id,branchId,terminalId,operationType,entityType,entityId,payload,status,attempts FROM SyncOutbox WHERE status='pending' OR (status='failed' AND (nextAttemptAt IS NULL OR nextAttemptAt <= @now)) ORDER BY occurredAt LIMIT @limit", new { limit, now = DateTime.Now }).ToList();
            }
        }

        public SyncStatus GetStatus()
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                return new SyncStatus
                {
                    Pending = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox WHERE status='pending'"),
                    Failed = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox WHERE status='failed'"),
                    Dead = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM SyncOutbox WHERE status='dead'"),
                    LastSyncedAt = connection.ExecuteScalar<DateTime?>("SELECT MAX(syncedAt) FROM SyncOutbox WHERE status='synced'"),
                    LastOccurredAt = connection.ExecuteScalar<DateTime?>("SELECT MAX(occurredAt) FROM SyncOutbox")
                };
            }
        }

        public void MarkAttempt(string id, string error)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var attempts = connection.ExecuteScalar<int>("SELECT attempts FROM SyncOutbox WHERE id=@id", new { id }) + 1;
                var dead = attempts >= MaxAttempts;
                var delay = (int)Math.Min(MaxBackoffSeconds, BaseBackoffSeconds * Math.Pow(2, Math.Min(attempts - 1, 20)));
                connection.Execute(
                    "UPDATE SyncOutbox SET status=@status, attempts=@attempts, lastError=@error, nextAttemptAt=@nextAttemptAt WHERE id=@id",
                    new
                    {
                        id,
                        status = dead ? "dead" : "failed",
                        attempts,
                        error = Truncate(error, 1000),
                        nextAttemptAt = dead ? (DateTime?)null : DateTime.Now.AddSeconds(delay)
                    });
            }
        }

        public void MarkSynced(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE SyncOutbox SET status='synced', syncedAt=@at, lastError=NULL WHERE id=@id", new { id, at = DateTime.Now });
        }

        public void Receive(SyncOperation operation)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                connection.Execute(@"INSERT OR IGNORE INTO SyncInbox
                    (operationId,branchId,terminalId,operationType,entityType,entityId,payload,receivedAt,status)
                    VALUES (@id,@branchId,@terminalId,@operationType,@entityType,@entityId,@payload,@receivedAt,'received')", new
                {
                    id = operation.Id,
                    branchId = operation.BranchId,
                    terminalId = operation.TerminalId,
                    operationType = operation.OperationType,
                    entityType = operation.EntityType,
                    entityId = operation.EntityId,
                    payload = operation.Payload,
                    receivedAt = DateTime.Now
                });
            }
        }

        public SyncApplyOutcome ApplyReceived(string operationId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var operation = connection.QueryFirstOrDefault<SyncOperation>("SELECT operationId AS id,branchId,terminalId,operationType,entityType,entityId,payload,status FROM SyncInbox WHERE operationId=@operationId", new { operationId }, transaction);
                if (operation == null)
                {
                    transaction.Rollback();
                    return SyncApplyOutcome.Failure("Operation not found in inbox");
                }
                if (operation.Status == "applied")
                {
                    transaction.Rollback();
                    return SyncApplyOutcome.Success();
                }
                try
                {
                    if (string.Equals(operation.EntityType, "Invoice", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var payload = JsonConvert.DeserializeObject<InvoiceSyncPayload>(operation.Payload);
                        if (payload != null && payload.Invoice != null)
                        {
                            var invoice = payload.Invoice;
                            connection.Execute(@"INSERT OR IGNORE INTO Invoice
                                (id,invoiceNumber,totalAmount,discount,discountType,discountValue,discountAmount,priceMode,status,clientId,employeeId,paymentMethod,createdBy,shiftId,branchId,terminalId,createdAt)
                                VALUES (@id,@invoiceNumber,@totalAmount,@discount,@discountType,@discountValue,@discountAmount,@priceMode,@status,@clientId,@employeeId,@paymentMethod,@createdBy,@shiftId,@branchId,@terminalId,@createdAt)", new
                            {
                                id = invoice.Id,
                                invoiceNumber = invoice.InvoiceNumber,
                                totalAmount = invoice.TotalAmount,
                                discount = invoice.Discount,
                                discountType = invoice.DiscountType,
                                discountValue = invoice.DiscountValue,
                                discountAmount = invoice.DiscountAmount,
                                priceMode = invoice.PriceMode,
                                status = invoice.Status ?? "posted",
                                clientId = invoice.ClientId,
                                employeeId = invoice.EmployeeId,
                                paymentMethod = invoice.PaymentMethod ?? "cash",
                                createdBy = invoice.CreatedBy,
                                shiftId = invoice.ShiftId,
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId,
                                createdAt = invoice.CreatedAt
                            }, transaction);

                            if (payload.Lines != null)
                            {
                                foreach (var line in payload.Lines)
                                {
                                    connection.Execute(@"INSERT OR IGNORE INTO InvoiceDetail
                                        (id,invoiceId,productId,productUnitId,unitName,quantity,buyPrice,salePrice,originalUnitPrice,unitPrice,discountType,discountValue,discountAmount,lineSubtotal,finalTotal,quantityFactor,totalCost,bundleComponentsJson,priceEditNote)
                                        VALUES (@id,@invoiceId,@productId,@productUnitId,@unitName,@quantity,@buyPrice,@salePrice,@originalUnitPrice,@unitPrice,@discountType,@discountValue,@discountAmount,@lineSubtotal,@finalTotal,@quantityFactor,@totalCost,@bundleComponentsJson,@priceEditNote)", new
                                    {
                                        id = string.IsNullOrEmpty(line.Id) ? Guid.NewGuid().ToString("N") : line.Id,
                                        invoiceId = invoice.Id,
                                        productId = line.ProductId,
                                        productUnitId = line.ProductUnitId,
                                        unitName = line.UnitName,
                                        quantity = line.Quantity,
                                        buyPrice = line.BuyPrice,
                                        salePrice = line.SalePrice,
                                        originalUnitPrice = line.OriginalUnitPrice,
                                        unitPrice = line.UnitPrice,
                                        discountType = line.DiscountType,
                                        discountValue = line.DiscountValue,
                                        discountAmount = line.DiscountAmount,
                                        lineSubtotal = line.LineSubtotal,
                                        finalTotal = line.FinalTotal,
                                        quantityFactor = line.QuantityFactor,
                                        totalCost = line.TotalCost,
                                        bundleComponentsJson = line.BundleComponentsJson,
                                        priceEditNote = line.PriceEditNote
                                    }, transaction);
                                }
                            }
                        }
                    }
                    else if (string.Equals(operation.EntityType, "Product", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var product = JsonConvert.DeserializeObject<Product>(operation.Payload);
                        if (product != null)
                        {
                            connection.Execute(@"INSERT INTO Product
                                (id,name,productType,serviceCost,buyPrice,stockQuantity,notes,allowDiscount,lowStockThreshold,isHiddenFromPOS,categoryId,brandId,createdAt,updatedAt)
                                VALUES (@id,@name,@productType,@serviceCost,@buyPrice,@stockQuantity,@notes,@allowDiscount,@lowStockThreshold,@isHiddenFromPOS,@categoryId,@brandId,@createdAt,@updatedAt)
                                ON CONFLICT(id) DO UPDATE SET name=@name,productType=@productType,
                                    serviceCost=@serviceCost,buyPrice=@buyPrice,notes=@notes,allowDiscount=@allowDiscount,
                                    lowStockThreshold=@lowStockThreshold,isHiddenFromPOS=@isHiddenFromPOS,
                                    categoryId=@categoryId,brandId=@brandId,updatedAt=@updatedAt", new
                            {
                                id = product.Id,
                                name = product.Name,
                                productType = product.ProductType ?? "product",
                                serviceCost = product.ServiceCost,
                                buyPrice = product.BuyPrice,
                                stockQuantity = product.StockQuantity,
                                notes = product.Notes,
                                allowDiscount = product.AllowDiscount ? 1 : 0,
                                lowStockThreshold = product.LowStockThreshold,
                                isHiddenFromPOS = product.IsHiddenFromPOS ? 1 : 0,
                                categoryId = product.CategoryId,
                                brandId = product.BrandId,
                                createdAt = product.CreatedAt,
                                updatedAt = product.UpdatedAt
                            }, transaction);
                        }
                    }
                    else if (string.Equals(operation.EntityType, "Category", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var category = JsonConvert.DeserializeObject<Category>(operation.Payload);
                        if (category != null)
                            connection.Execute(@"INSERT INTO Category (id,name,description,isActive,createdAt,updatedAt)
                                VALUES (@id,@name,@description,@isActive,@createdAt,@updatedAt)
                                ON CONFLICT(id) DO UPDATE SET name=@name,description=@description,isActive=@isActive,updatedAt=@updatedAt", new
                            {
                                id = category.Id, name = category.Name, description = category.Description,
                                isActive = category.IsActive ? 1 : 0, createdAt = category.CreatedAt, updatedAt = category.UpdatedAt
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "ProductUnit", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var unit = JsonConvert.DeserializeObject<ProductUnit>(operation.Payload);
                        if (unit != null)
                            connection.Execute(@"INSERT INTO ProductUnit (id,productId,unitName,unitId,quantityFactor,retailPrice,wholesalePrice,isBaseUnit,createdAt)
                                VALUES (@id,@productId,@unitName,@unitId,@quantityFactor,@retailPrice,@wholesalePrice,@isBaseUnit,@createdAt)
                                ON CONFLICT(id) DO UPDATE SET unitName=@unitName,unitId=@unitId,quantityFactor=@quantityFactor,
                                    retailPrice=@retailPrice,wholesalePrice=@wholesalePrice,isBaseUnit=@isBaseUnit", new
                            {
                                id = unit.Id, productId = unit.ProductId, unitName = unit.UnitName, unitId = unit.UnitId,
                                quantityFactor = unit.QuantityFactor, retailPrice = unit.RetailPrice, wholesalePrice = unit.WholesalePrice,
                                isBaseUnit = unit.IsBaseUnit ? 1 : 0, createdAt = unit.CreatedAt
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "Brand", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var brand = JsonConvert.DeserializeObject<Brand>(operation.Payload);
                        if (brand != null)
                            connection.Execute(@"INSERT INTO Brand (id,name,isActive,createdAt,updatedAt)
                                VALUES (@id,@name,@isActive,@createdAt,@updatedAt)
                                ON CONFLICT(id) DO UPDATE SET name=@name,isActive=@isActive,updatedAt=@updatedAt", new
                            {
                                id = brand.Id, name = brand.Name, isActive = brand.IsActive ? 1 : 0,
                                createdAt = brand.CreatedAt, updatedAt = brand.UpdatedAt
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "Payment", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var payment = JsonConvert.DeserializeObject<PaymentSyncPayload>(operation.Payload);
                        if (payment != null)
                            connection.Execute(@"INSERT OR IGNORE INTO Payment
                                (id,amount,paymentMethod,date,invoiceId,clientId,supplierId,reference,notes,createdBy,shiftId,branchId,terminalId,createdAt)
                                VALUES (@id,@amount,@paymentMethod,@date,@invoiceId,@clientId,@supplierId,@reference,@notes,@createdBy,@shiftId,@branchId,@terminalId,@createdAt)", new
                            {
                                id = payment.Id,
                                amount = payment.Amount,
                                paymentMethod = payment.PaymentMethod ?? "cash",
                                date = payment.Date,
                                invoiceId = payment.InvoiceId,
                                clientId = payment.ClientId,
                                supplierId = payment.SupplierId,
                                reference = payment.Reference,
                                notes = payment.Notes,
                                createdBy = payment.CreatedBy,
                                shiftId = payment.ShiftId,
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId,
                                createdAt = payment.CreatedAt
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "Shift", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var shift = JsonConvert.DeserializeObject<Shift>(operation.Payload);
                        if (shift != null)
                            connection.Execute(@"INSERT OR IGNORE INTO Shift
                                (id,number,openedBy,openingCash,openedAt,closedAt,countedCash,expectedCash,difference,notes,status,branchId,terminalId)
                                VALUES (@id,@number,@openedBy,@openingCash,@openedAt,@closedAt,@countedCash,@expectedCash,@difference,@notes,@status,@branchId,@terminalId)", new
                            {
                                id = shift.Id,
                                number = shift.Number,
                                openedBy = shift.OpenedBy,
                                openingCash = shift.OpeningCash,
                                openedAt = shift.OpenedAt,
                                closedAt = shift.ClosedAt,
                                countedCash = shift.CountedCash,
                                expectedCash = shift.ExpectedCash,
                                difference = shift.Difference,
                                notes = shift.Notes,
                                status = shift.Status ?? "open",
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "Expense", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var expense = JsonConvert.DeserializeObject<Expense>(operation.Payload);
                        if (expense != null)
                            connection.Execute(@"INSERT OR IGNORE INTO Expense
                                (id,categoryId,amount,paymentMethod,date,description,reference,shiftId,createdBy,createdAt,branchId,terminalId)
                                VALUES (@id,@categoryId,@amount,@paymentMethod,@date,@description,@reference,@shiftId,@createdBy,@createdAt,@branchId,@terminalId)", new
                            {
                                id = expense.Id,
                                categoryId = expense.CategoryId,
                                amount = expense.Amount,
                                paymentMethod = expense.PaymentMethod ?? "cash",
                                date = expense.Date,
                                description = expense.Description,
                                reference = expense.Reference,
                                shiftId = expense.ShiftId,
                                createdBy = expense.CreatedBy,
                                createdAt = expense.CreatedAt,
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "StockMovement", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var movement = JsonConvert.DeserializeObject<StockMovement>(operation.Payload);
                        if (movement != null)
                            connection.Execute(@"INSERT OR IGNORE INTO StockMovement
                                (id,productId,quantity,type,referenceId,referenceNumber,createdAt,branchId,terminalId)
                                VALUES (@id,@productId,@quantity,@type,@referenceId,@referenceNumber,@createdAt,@branchId,@terminalId)", new
                            {
                                id = movement.Id,
                                productId = movement.ProductId,
                                quantity = movement.Quantity,
                                type = movement.Type,
                                referenceId = movement.ReferenceId,
                                referenceNumber = movement.ReferenceNumber,
                                createdAt = movement.CreatedAt,
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId
                            }, transaction);
                    }
                    else if (string.Equals(operation.EntityType, "SaleReturn", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(operation.Payload))
                    {
                        var returnPayload = JsonConvert.DeserializeObject<SaleReturnSyncPayload>(operation.Payload);
                        if (returnPayload != null && returnPayload.SaleReturn != null)
                        {
                            var saleReturn = returnPayload.SaleReturn;
                            var invoiceExists = connection.ExecuteScalar<int>(
                                "SELECT COUNT(1) FROM Invoice WHERE id=@id", new { id = saleReturn.InvoiceId }, transaction) > 0;
                            if (!invoiceExists)
                                throw new InvalidOperationException("Referenced invoice has not synced yet");

                            connection.Execute(@"INSERT OR IGNORE INTO SaleReturn
                                (id,number,invoiceId,date,totalAmount,restoredCost,paymentMethod,notes,status,createdBy,createdAt,branchId,terminalId)
                                VALUES (@id,@number,@invoiceId,@date,@totalAmount,@restoredCost,@paymentMethod,@notes,@status,@createdBy,@createdAt,@branchId,@terminalId)", new
                            {
                                id = saleReturn.Id,
                                number = saleReturn.Number,
                                invoiceId = saleReturn.InvoiceId,
                                date = saleReturn.Date,
                                totalAmount = saleReturn.TotalAmount,
                                restoredCost = saleReturn.RestoredCost,
                                paymentMethod = saleReturn.PaymentMethod ?? "cash",
                                notes = saleReturn.Notes,
                                status = saleReturn.Status ?? "posted",
                                createdBy = saleReturn.CreatedBy,
                                createdAt = saleReturn.CreatedAt,
                                branchId = operation.BranchId,
                                terminalId = operation.TerminalId
                            }, transaction);

                            if (returnPayload.Details != null)
                            {
                                foreach (var detail in returnPayload.Details)
                                {
                                    connection.Execute(@"INSERT OR IGNORE INTO SaleReturnDetail
                                        (id,returnId,invoiceDetailId,productId,unitName,quantity,quantityFactor,unitPrice,lineTotal,restoredCost)
                                        VALUES (@id,@returnId,@invoiceDetailId,@productId,@unitName,@quantity,@quantityFactor,@unitPrice,@lineTotal,@restoredCost)", new
                                    {
                                        id = string.IsNullOrEmpty(detail.Id) ? Guid.NewGuid().ToString("N") : detail.Id,
                                        returnId = saleReturn.Id,
                                        invoiceDetailId = detail.InvoiceDetailId,
                                        productId = detail.ProductId,
                                        unitName = detail.UnitName,
                                        quantity = detail.Quantity,
                                        quantityFactor = detail.QuantityFactor,
                                        unitPrice = detail.UnitPrice,
                                        lineTotal = detail.LineTotal,
                                        restoredCost = detail.RestoredCost
                                    }, transaction);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(returnPayload.InvoiceReturnStatus))
                                connection.Execute("UPDATE Invoice SET returnStatus=@status WHERE id=@invoiceId",
                                    new { status = returnPayload.InvoiceReturnStatus, invoiceId = saleReturn.InvoiceId }, transaction);
                        }
                    }

                    connection.Execute("UPDATE SyncInbox SET status='applied',appliedAt=@at,lastError=NULL WHERE operationId=@operationId", new { operationId, at = DateTime.Now }, transaction);
                    transaction.Commit();
                    return SyncApplyOutcome.Success();
                }
                catch (Exception ex)
                {
                    try
                    {
                        connection.Execute("UPDATE SyncInbox SET status='failed',lastError=@error WHERE operationId=@operationId", new { operationId, error = Truncate(ex.Message, 1000) }, transaction);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                    return SyncApplyOutcome.Failure(ex.Message);
                }
            }
        }

        public List<SyncChange> GetChanges(long afterVersion, int limit, string branchId = null)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                return connection.Query<SyncChange>(@"SELECT version,branchId,entityType,entityId,operationType,payload,createdAt
                    FROM SyncChangeLog
                    WHERE version > @afterVersion
                      AND (@branchId IS NULL OR branchId IS NULL OR branchId = @branchId)
                    ORDER BY version LIMIT @limit", new { afterVersion, limit, branchId }).ToList();
            }
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private sealed class InvoiceSyncPayload
        {
            public Invoice Invoice { get; set; }
            public List<InvoiceDetail> Lines { get; set; }
        }

        private sealed class PaymentSyncPayload
        {
            public string Id { get; set; }
            public double Amount { get; set; }
            public string PaymentMethod { get; set; }
            public DateTime Date { get; set; }
            public string InvoiceId { get; set; }
            public string ClientId { get; set; }
            public string SupplierId { get; set; }
            public string Reference { get; set; }
            public string Notes { get; set; }
            public string CreatedBy { get; set; }
            public string ShiftId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class SaleReturnSyncPayload
        {
            public SaleReturn SaleReturn { get; set; }
            public List<SaleReturnDetail> Details { get; set; }
            public string InvoiceReturnStatus { get; set; }
        }
    }
}
