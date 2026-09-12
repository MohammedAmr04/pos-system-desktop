using PosCs.Application.Models;
using PosCs.Domain.Entities;
using System.Collections.Generic;

namespace PosCs.Application.Ports
{
    /// <summary>
    /// Categories are never deleted — deactivation preserves Product.categoryId (spec §2.2/§3.3).
    /// </summary>
    public interface ICategoryRepository
    {
        List<Category> GetAll();
        PagedResult<Category> GetPaged(int page, int pageSize, string query);
        Category GetById(string id);
        Category GetByName(string name);
        Category Create(Category category);
        Category Update(Category category);
        int CountProducts(string categoryId);
    }

    /// <summary>
    /// Brands may be deleted only when no product references them; otherwise deactivate.
    /// </summary>
    public interface IBrandRepository
    {
        List<Brand> GetAll();
        PagedResult<Brand> GetPaged(int page, int pageSize, string query);
        Brand GetById(string id);
        Brand GetByName(string name);
        Brand Create(Brand brand);
        Brand Update(Brand brand);
        bool Delete(string id);
        int CountProducts(string brandId);
    }

    /// <summary>
    /// Shared unit master. Units referenced by any ProductUnit cannot be deleted.
    /// </summary>
    public interface IUnitRepository
    {
        List<Unit> GetAll();
        PagedResult<Unit> GetPaged(int page, int pageSize, string query);
        Unit GetById(string id);
        Unit GetByName(string name);
        Unit Create(Unit unit);
        Unit Update(Unit unit);
        bool Delete(string id);
        int CountProductUnits(string unitId);
    }
}
