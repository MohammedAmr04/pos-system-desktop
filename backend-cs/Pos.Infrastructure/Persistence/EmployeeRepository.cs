using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private const string Columns = "id, name, phone, isActive, createdAt, updatedAt";

        public List<Employee> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Employee>("SELECT * FROM Employee ORDER BY name COLLATE NOCASE").ToList();
        }

        public PagedResult<Employee> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Employee");
                    var items = conn.Query<Employee>(
                        "SELECT * FROM Employee ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Employee> { Items = items, Total = total };
                }

                var like = "%" + EscapeLike(query) + "%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Employee WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\'",
                    new { like });
                var filteredItems = conn.Query<Employee>(
                    "SELECT * FROM Employee WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Employee> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public Employee GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Employee>("SELECT * FROM Employee WHERE id = @id", new { id });
        }

        public Employee Create(Employee employee)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                employee.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                employee.CreatedAt = now;
                employee.UpdatedAt = now;
                conn.Execute(@"
                    INSERT INTO Employee (id, name, phone, isActive, createdAt, updatedAt)
                    VALUES (@id, @name, @phone, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = employee.Id,
                        name = employee.Name,
                        phone = employee.Phone,
                        isActive = employee.IsActive ? 1 : 0,
                        createdAt = employee.CreatedAt,
                        updatedAt = employee.UpdatedAt
                    });
                return employee;
            }
        }

        public Employee Update(Employee employee)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                employee.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Employee SET name = @name, phone = @phone,
                        isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = employee.Id,
                        name = employee.Name,
                        phone = employee.Phone,
                        isActive = employee.IsActive ? 1 : 0,
                        updatedAt = employee.UpdatedAt
                    });
                return employee;
            }
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
