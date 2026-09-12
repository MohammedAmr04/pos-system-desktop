using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>
    /// Employee directory use cases. Employees are deactivated once referenced — never
    /// deleted, because posted invoices need a stable answer to "who made this sale?".
    /// Names are intentionally NOT unique: different employees may share a name.
    /// </summary>
    public class EmployeeService
    {
        private readonly IEmployeeRepository _repo;

        public EmployeeService(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        /// <summary>Active-only list for assignment pickers (inactive employees are not selectable).</summary>
        public List<Employee> GetActive()
        {
            return _repo.GetAll().Where(e => e.IsActive).ToList();
        }

        public PagedResult<Employee> GetPaged(int page, int pageSize, string query)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query == null ? null : query.Trim());
        }

        public Employee GetById(string id)
        {
            var employee = _repo.GetById(id);
            if (employee == null)
                throw new NotFoundException("Employee not found");
            return employee;
        }

        public Employee Create(CreateEmployeeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Employee name is required");

            return _repo.Create(new Employee
            {
                Name = request.Name.Trim(),
                Phone = Clean(request.Phone),
                IsActive = true
            });
        }

        public Employee Update(string id, UpdateEmployeeRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid employee data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Employee not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                existing.Name = request.Name.Trim();

            // Null keeps the current value; empty string clears it.
            if (request.Phone != null)
                existing.Phone = Clean(request.Phone);
            if (request.IsActive.HasValue)
                existing.IsActive = request.IsActive.Value;

            return _repo.Update(existing);
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
