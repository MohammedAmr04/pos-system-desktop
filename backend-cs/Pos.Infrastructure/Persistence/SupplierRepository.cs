using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class SupplierRepository : ISupplierRepository
    {
        private const string Columns = "id, name, phone, address, notes, isActive, createdAt, updatedAt";

        public List<Supplier> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Supplier>("SELECT * FROM Supplier ORDER BY name COLLATE NOCASE").ToList();
        }

        /// <summary>
        /// Balance mirrors SupplierService.GetStatement exactly: posted credit
        /// purchases minus non-cash-origin returns minus non-negative payments.
        /// Rounded to 2 decimals so the zero filter is not tripped by float noise.
        /// </summary>
        private const string BalanceExpr =
            "ROUND(COALESCE((SELECT SUM(total) FROM PurchaseInvoice WHERE supplierId = s.id AND status = 'posted' AND paymentMethod <> 'cash'), 0) " +
            "- COALESCE((SELECT SUM(r.totalAmount) FROM PurchaseReturn r JOIN PurchaseInvoice p ON p.id = r.purchaseInvoiceId " +
            "WHERE p.supplierId = s.id AND p.paymentMethod <> 'cash' AND r.status = 'posted'), 0) " +
            "- COALESCE((SELECT SUM(amount) FROM Payment WHERE supplierId = s.id AND amount >= 0), 0), 2)";

        public PagedResult<Supplier> GetPaged(int page, int pageSize, string query, string balanceFilter)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    clauses.Add("(s.name LIKE @like ESCAPE '\\' OR s.phone LIKE @like ESCAPE '\\')");
                    parameters.Add("like", $"%{EscapeLike(query)}%");
                }

                if (balanceFilter == "positive")
                    clauses.Add($"({BalanceExpr}) > 0");
                else if (balanceFilter == "negative")
                    clauses.Add($"({BalanceExpr}) < 0");
                else if (balanceFilter == "zero")
                    clauses.Add($"({BalanceExpr}) = 0");

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Supplier s{where}", parameters);
                var items = conn.Query<Supplier>(
                    $"SELECT s.*, ({BalanceExpr}) AS Balance FROM Supplier s{where} " +
                    "ORDER BY s.name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();
                return new PagedResult<Supplier> { Items = items, Total = total };
            }
        }

        public Supplier GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Supplier>("SELECT * FROM Supplier WHERE id = @id", new { id });
        }

        public Supplier Create(Supplier supplier)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                supplier.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                supplier.CreatedAt = now;
                supplier.UpdatedAt = now;
                conn.Execute($@"
                    INSERT INTO Supplier ({Columns})
                    VALUES (@id, @name, @phone, @address, @notes, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = supplier.Id,
                        name = supplier.Name,
                        phone = supplier.Phone,
                        address = supplier.Address,
                        notes = supplier.Notes,
                        isActive = supplier.IsActive ? 1 : 0,
                        createdAt = supplier.CreatedAt,
                        updatedAt = supplier.UpdatedAt
                    });
                return supplier;
            }
        }

        public Supplier Update(Supplier supplier)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                supplier.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Supplier SET name = @name, phone = @phone, address = @address,
                        notes = @notes, isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = supplier.Id,
                        name = supplier.Name,
                        phone = supplier.Phone,
                        address = supplier.Address,
                        notes = supplier.Notes,
                        isActive = supplier.IsActive ? 1 : 0,
                        updatedAt = supplier.UpdatedAt
                    });
                return supplier;
            }
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
