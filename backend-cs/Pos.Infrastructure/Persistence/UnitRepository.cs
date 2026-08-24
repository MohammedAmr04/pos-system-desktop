using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class UnitRepository : IUnitRepository
    {
        public List<Unit> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Unit>("SELECT * FROM Unit ORDER BY name COLLATE NOCASE").ToList();
        }

        public PagedResult<Unit> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Unit");
                    var items = conn.Query<Unit>(
                        "SELECT * FROM Unit ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Unit> { Items = items, Total = total };
                }

                var like = $"%{EscapeLike(query)}%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Unit WHERE name LIKE @like ESCAPE '\\'", new { like });
                var filteredItems = conn.Query<Unit>(
                    "SELECT * FROM Unit WHERE name LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Unit> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public Unit GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Unit>("SELECT * FROM Unit WHERE id = @id", new { id });
        }

        public Unit GetByName(string name)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Unit>(
                    "SELECT * FROM Unit WHERE name = @name COLLATE NOCASE", new { name });
        }

        public Unit Create(Unit unit)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                unit.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                unit.CreatedAt = now;
                unit.UpdatedAt = now;
                conn.Execute(@"
                    INSERT INTO Unit (id, name, isActive, createdAt, updatedAt)
                    VALUES (@id, @name, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = unit.Id,
                        name = unit.Name,
                        isActive = unit.IsActive ? 1 : 0,
                        createdAt = unit.CreatedAt,
                        updatedAt = unit.UpdatedAt
                    });
                return unit;
            }
        }

        public Unit Update(Unit unit)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                unit.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Unit SET name = @name, isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = unit.Id,
                        name = unit.Name,
                        isActive = unit.IsActive ? 1 : 0,
                        updatedAt = unit.UpdatedAt
                    });
                return unit;
            }
        }

        public bool Delete(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Execute("DELETE FROM Unit WHERE id = @id", new { id }) > 0;
        }

        public int CountProductUnits(string unitId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM ProductUnit WHERE unitId = @unitId",
                    new { unitId });
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
