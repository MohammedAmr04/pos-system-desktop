using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class CategoryRepository : ICategoryRepository
    {
        public List<Category> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Category>("SELECT * FROM Category ORDER BY name COLLATE NOCASE").ToList();
        }

        public PagedResult<Category> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Category");
                    var items = conn.Query<Category>(
                        "SELECT * FROM Category ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Category> { Items = items, Total = total };
                }

                var like = $"%{EscapeLike(query)}%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Category WHERE name LIKE @like ESCAPE '\\'", new { like });
                var filteredItems = conn.Query<Category>(
                    "SELECT * FROM Category WHERE name LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Category> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public Category GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Category>("SELECT * FROM Category WHERE id = @id", new { id });
        }

        public Category GetByName(string name)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Category>(
                    "SELECT * FROM Category WHERE name = @name COLLATE NOCASE", new { name });
        }

        public Category Create(Category category)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                category.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                category.CreatedAt = now;
                category.UpdatedAt = now;
                conn.Execute(@"
                    INSERT INTO Category (id, name, description, isActive, createdAt, updatedAt)
                    VALUES (@id, @name, @description, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = category.Id,
                        name = category.Name,
                        description = category.Description,
                        isActive = category.IsActive ? 1 : 0,
                        createdAt = category.CreatedAt,
                        updatedAt = category.UpdatedAt
                    });
                return category;
            }
        }

        public Category Update(Category category)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                category.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Category SET name = @name, description = @description,
                        isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = category.Id,
                        name = category.Name,
                        description = category.Description,
                        isActive = category.IsActive ? 1 : 0,
                        updatedAt = category.UpdatedAt
                    });
                return category;
            }
        }

        public int CountProducts(string categoryId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product WHERE categoryId = @categoryId",
                    new { categoryId });
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
