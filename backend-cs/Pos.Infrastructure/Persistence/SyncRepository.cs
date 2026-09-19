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
        public List<SyncOperation> GetPending(int limit)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                return connection.Query<SyncOperation>(@"SELECT id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status,attempts,lastError,syncedAt
                    FROM SyncOutbox WHERE status IN ('pending','failed') ORDER BY occurredAt LIMIT @limit", new { limit }).ToList();
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
                    LastSyncedAt = connection.ExecuteScalar<DateTime?>("SELECT MAX(syncedAt) FROM SyncOutbox WHERE status='synced'"),
                    LastOccurredAt = connection.ExecuteScalar<DateTime?>("SELECT MAX(occurredAt) FROM SyncOutbox")
                };
            }
        }

        public void MarkAttempt(string id, string error)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                connection.Execute(@"UPDATE SyncOutbox SET status='failed', attempts=attempts+1,
                    lastError=@error WHERE id=@id", new { id, error = Truncate(error, 1000) });
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
                    operation.BranchId,
                    operation.TerminalId,
                    operation.OperationType,
                    operation.EntityType,
                    operation.EntityId,
                    operation.Payload,
                    receivedAt = DateTime.Now
                });
            }
        }

        public void ApplyReceived(string operationId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var operation = connection.QueryFirstOrDefault<SyncOperation>("SELECT operationId AS id,branchId,terminalId,operationType,entityType,entityId,payload,status FROM SyncInbox WHERE operationId=@operationId", new { operationId }, transaction);
                if (operation == null || operation.Status == "applied") return;
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

                    connection.Execute("UPDATE SyncInbox SET status='applied',appliedAt=@at,lastError=NULL WHERE operationId=@operationId", new { operationId, at = DateTime.Now }, transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    connection.Execute("UPDATE SyncInbox SET status='failed',lastError=@error WHERE operationId=@operationId", new { operationId, error = ex.Message }, transaction);
                    transaction.Commit();
                }
            }
        }

        public List<SyncChange> GetChanges(long afterVersion, int limit)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                return connection.Query<SyncChange>(@"SELECT version,branchId,entityType,entityId,operationType,payload,createdAt
                    FROM SyncChangeLog WHERE version > @afterVersion ORDER BY version LIMIT @limit", new { afterVersion, limit }).ToList();
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
    }
}
