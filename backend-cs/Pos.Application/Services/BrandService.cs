using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Brand use cases. Referenced brands can only be deactivated, not deleted (spec §4.3).</summary>
    public class BrandService
    {
        private readonly IBrandRepository _repo;

        public BrandService(IBrandRepository repo)
        {
            _repo = repo;
        }

        public List<Brand> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>Active-only list for assignment pickers (inactive brands are not selectable).</summary>
        public List<Brand> GetActive()
        {
            return _repo.GetAll().Where(b => b.IsActive).ToList();
        }

        public PagedResult<Brand> GetPaged(int page, int pageSize, string query)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query?.Trim());
        }

        public Brand GetById(string id)
        {
            var brand = _repo.GetById(id);
            if (brand == null)
                throw new NotFoundException("Brand not found");
            return brand;
        }

        public Brand Create(CreateBrandRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Brand name is required");

            var name = request.Name.Trim();
            if (_repo.GetByName(name) != null)
                throw new DomainValidationException("A brand with this name already exists");

            return _repo.Create(new Brand
            {
                Name = name,
                IsActive = true
            });
        }

        public Brand Update(string id, UpdateBrandRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid brand data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Brand not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();
                var duplicate = _repo.GetByName(name);
                if (duplicate != null && duplicate.Id != id)
                    throw new DomainValidationException("A brand with this name already exists");
                existing.Name = name;
            }

            if (request.IsActive.HasValue)
                existing.IsActive = request.IsActive.Value;

            return _repo.Update(existing);
        }

        public void Delete(string id)
        {
            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Brand not found");

            // Deactivate != Delete != Detach: referenced brands must stay resolvable.
            if (_repo.CountProducts(id) > 0)
                throw new DomainValidationException("Cannot delete a brand that is referenced by products. Deactivate it instead.");

            if (!_repo.Delete(id))
                throw new InvalidOperationException("Failed to delete brand");
        }
    }
}
