using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Shared unit master use cases (spec §5). Used units cannot be deleted.</summary>
    public class UnitMasterService
    {
        private readonly IUnitRepository _repo;
        private readonly IProductUnitRepository _productUnits;

        public UnitMasterService(IUnitRepository repo, IProductUnitRepository productUnits)
        {
            _repo = repo;
            _productUnits = productUnits;
        }

        public List<Unit> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>Active-only list for product-unit pickers.</summary>
        public List<Unit> GetActive()
        {
            return _repo.GetAll().Where(u => u.IsActive).ToList();
        }

        public PagedResult<Unit> GetPaged(int page, int pageSize, string query)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query?.Trim());
        }

        public Unit GetById(string id)
        {
            var unit = _repo.GetById(id);
            if (unit == null)
                throw new NotFoundException("Unit not found");
            return unit;
        }

        public Unit Create(CreateUnitRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Unit name is required");

            var name = request.Name.Trim();
            if (_repo.GetByName(name) != null)
                throw new DomainValidationException("A unit with this name already exists");

            return _repo.Create(new Unit
            {
                Name = name,
                IsActive = true
            });
        }

        public Unit Update(string id, UpdateMasterUnitRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid unit data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Unit not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();
                var duplicate = _repo.GetByName(name);
                if (duplicate != null && duplicate.Id != id)
                    throw new DomainValidationException("A unit with this name already exists");
                existing.Name = name;
            }

            if (request.IsActive.HasValue && !request.IsActive.Value && existing.IsActive
                && _repo.CountProductUnits(id) > 0)
                throw new DomainValidationException("Cannot deactivate a unit that is used by products.");

            if (request.IsActive.HasValue)
                existing.IsActive = request.IsActive.Value;

            var updated = _repo.Update(existing);

            if (!string.IsNullOrWhiteSpace(request.Name))
                _productUnits.SyncUnitName(id, existing.Name);

            return updated;
        }

        public void Delete(string id)
        {
            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Unit not found");

            if (_repo.CountProductUnits(id) > 0)
                throw new DomainValidationException("Cannot delete a unit that is used by products. Deactivate it instead.");

            if (!_repo.Delete(id))
                throw new InvalidOperationException("Failed to delete unit");
        }
    }
}
