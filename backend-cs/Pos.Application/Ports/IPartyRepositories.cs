using PosCs.Application.Models;
using PosCs.Domain.Entities;
using System.Collections.Generic;

namespace PosCs.Application.Ports
{
    /// <summary>
    /// Suppliers are never deleted — once purchases reference them the identity must be
    /// preserved, so they are deactivated instead (spec §9.2 rule 7).
    /// </summary>
    public interface ISupplierRepository
    {
        List<Supplier> GetAll();
        /// <summary>
        /// Server-side paged listing. Each item carries its computed Balance.
        /// balanceFilter: null/"all" (no filter), "positive" (we owe them),
        /// "negative" (they owe us / overpaid), "zero" (settled).
        /// </summary>
        PagedResult<Supplier> GetPaged(int page, int pageSize, string query, string balanceFilter);
        Supplier GetById(string id);
        Supplier Create(Supplier supplier);
        Supplier Update(Supplier supplier);
    }

    /// <summary>
    /// Clients are never deleted — credit sales require a stable identity to answer
    /// "who owes us money?" (spec §10.2).
    /// </summary>
    public interface IClientRepository
    {
        List<Client> GetAll();
        /// <summary>
        /// Server-side paged listing. Each item carries its computed Balance.
        /// balanceFilter: null/"all" (no filter), "positive" (they owe us),
        /// "negative" (advance credit — we owe them), "zero" (settled).
        /// </summary>
        PagedResult<Client> GetPaged(int page, int pageSize, string query, string balanceFilter);
        Client GetById(string id);
        Client Create(Client client);
        Client Update(Client client);
    }

    /// <summary>
    /// Employees are never deleted — invoices reference them for sales attribution,
    /// so they are deactivated instead.
    /// </summary>
    public interface IEmployeeRepository
    {
        List<Employee> GetAll();
        PagedResult<Employee> GetPaged(int page, int pageSize, string query);
        Employee GetById(string id);
        Employee Create(Employee employee);
        Employee Update(Employee employee);
    }
}
