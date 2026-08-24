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

        public PagedResult<Supplier> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Supplier");
                    var items = conn.Query<Supplier>(
                        "SELECT * FROM Supplier ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Supplier> { Items = items, Total = total };
                }

                var like = $"%{EscapeLike(query)}%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Supplier WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\'",
                    new { like });
                var filteredItems = conn.Query<Supplier>(
                    "SELECT * FROM Supplier WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Supplier> { Items = filteredItems, Total = totalFiltered };
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
