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
        PagedResult<Supplier> GetPaged(int page, int pageSize, string query);
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
        PagedResult<Client> GetPaged(int page, int pageSize, string query);
        Client GetById(string id);
        Client Create(Client client);
        Client Update(Client client);
    }
}
