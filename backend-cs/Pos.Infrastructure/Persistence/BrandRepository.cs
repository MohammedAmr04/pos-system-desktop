using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class BrandRepository : IBrandRepository
    {
        public List<Brand> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Brand>("SELECT * FROM Brand ORDER BY name COLLATE NOCASE").ToList();
        }

        public PagedResult<Brand> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Brand");
                    var items = conn.Query<Brand>(
                        "SELECT * FROM Brand ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Brand> { Items = items, Total = total };
                }

                var like = $"%{EscapeLike(query)}%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Brand WHERE name LIKE @like ESCAPE '\\'", new { like });
                var filteredItems = conn.Query<Brand>(
                    "SELECT * FROM Brand WHERE name LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Brand> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public Brand GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Brand>("SELECT * FROM Brand WHERE id = @id", new { id });
        }

        public Brand GetByName(string name)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Brand>(
                    "SELECT * FROM Brand WHERE name = @name COLLATE NOCASE", new { name });
        }

        public Brand Create(Brand brand)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                brand.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                brand.CreatedAt = now;
                brand.UpdatedAt = now;
                conn.Execute(@"
                    INSERT INTO Brand (id, name, isActive, createdAt, updatedAt)
                    VALUES (@id, @name, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = brand.Id,
                        name = brand.Name,
                        isActive = brand.IsActive ? 1 : 0,
                        createdAt = brand.CreatedAt,
                        updatedAt = brand.UpdatedAt
                    });
                return brand;
            }
        }

        public Brand Update(Brand brand)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                brand.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Brand SET name = @name, isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = brand.Id,
                        name = brand.Name,
                        isActive = brand.IsActive ? 1 : 0,
                        updatedAt = brand.UpdatedAt
                    });
                return brand;
            }
        }

        public bool Delete(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Execute("DELETE FROM Brand WHERE id = @id", new { id }) > 0;
        }

        public int CountProducts(string brandId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product WHERE brandId = @brandId",
                    new { brandId });
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
