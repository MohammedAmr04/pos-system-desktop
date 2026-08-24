using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Category use cases. Categories are deactivated, never deleted (spec §3.3).</summary>
    public class CategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public List<Category> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>Active-only list for assignment pickers (inactive categories are not selectable).</summary>
        public List<Category> GetActive()
        {
            return _repo.GetAll().Where(c => c.IsActive).ToList();
        }

        public PagedResult<Category> GetPaged(int page, int pageSize, string query)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query?.Trim());
        }

        public Category GetById(string id)
        {
            var category = _repo.GetById(id);
            if (category == null)
                throw new NotFoundException("Category not found");
            return category;
        }

        public Category Create(CreateCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Category name is required");

            var name = request.Name.Trim();
            if (_repo.GetByName(name) != null)
                throw new DomainValidationException("A category with this name already exists");

            return _repo.Create(new Category
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = true
            });
        }

        public Category Update(string id, UpdateCategoryRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid category data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Category not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();
                var duplicate = _repo.GetByName(name);
                if (duplicate != null && duplicate.Id != id)
                    throw new DomainValidationException("A category with this name already exists");
                existing.Name = name;
            }

            // Empty string clears the description; null keeps it.
            if (request.Description != null)
                existing.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            if (request.IsActive.HasValue)
                existing.IsActive = request.IsActive.Value;

            return _repo.Update(existing);
        }
    }
}
