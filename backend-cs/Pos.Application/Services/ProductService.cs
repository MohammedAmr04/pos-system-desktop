using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>Catalog use cases: products, units and barcodes.</summary>
    public class ProductService
    {
        private readonly IProductRepository _repo;
        private readonly IProductUnitRepository _unitRepo;
        private readonly IUnitRepository _masterUnits;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IBrandRepository _brandRepo;

        public ProductService(IProductRepository repo, IProductUnitRepository unitRepo,
            ICategoryRepository categoryRepo, IBrandRepository brandRepo, IUnitRepository masterUnits)
        {
            _repo = repo;
            _unitRepo = unitRepo;
            _categoryRepo = categoryRepo;
            _brandRepo = brandRepo;
            _masterUnits = masterUnits;
        }

        public List<Product> GetAll()
        {
            var products = _repo.GetAll();
            AttachUnits(products);
            return products;
        }

        public Product GetById(string id)
        {
            var product = _repo.GetById(id);
            if (product == null)
                throw new NotFoundException("Product not found");
            AttachUnits(product);
            return product;
        }

        public List<Product> Search(string q, int limit)
        {
            if (string.IsNullOrWhiteSpace(q))
                return new List<Product>();
            var results = _repo.Search(q.Trim(), Math.Max(1, Math.Min(limit, 100)));
            AttachUnits(results);
            return results;
        }

        public PagedResult<Product> GetPaged(int page, int pageSize, string q)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            var result = _repo.GetPaged(page, pageSize, q?.Trim());
            AttachUnits(result.Items);
            return result;
        }

        public int Count()
        {
            return _repo.Count();
        }

        public Product Create(CreateProductRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Product name is required");

            var retailPrice = request.RetailPrice > 0 ? request.RetailPrice : request.SalePrice;
            if (retailPrice <= 0)
                throw new DomainValidationException("Retail price is required");
            if (retailPrice < 0)
                throw new DomainValidationException("Retail price cannot be negative");
            if (request.WholesalePrice.HasValue && request.WholesalePrice.Value < 0)
                throw new DomainValidationException("Wholesale price cannot be negative");

            var unitName = string.IsNullOrWhiteSpace(request.UnitName) ? "Piece" : request.UnitName.Trim();
            var baseUnitId = ResolveUnitRef(request.UnitId, ref unitName);

            var barcode = request.Barcode?.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
                barcode = _unitRepo.GenerateUniqueBarcode();
            else if (_unitRepo.BarcodeExists(barcode))
                throw new DomainValidationException("Barcode already in use");

            var product = _repo.CreateWithBaseUnit(new Product
            {
                Name = request.Name,
                BuyPrice = request.BuyPrice,
                StockQuantity = request.StockQuantity,
                Notes = request.Notes,
                AllowDiscount = request.AllowDiscount,
                LowStockThreshold = request.LowStockThreshold,
                CategoryId = ResolveCategoryRef(request.CategoryId, null),
                BrandId = ResolveBrandRef(request.BrandId, null)
            }, new ProductUnit
            {
                UnitName = unitName,
                UnitId = baseUnitId,
                QuantityFactor = 1,
                RetailPrice = retailPrice,
                WholesalePrice = request.WholesalePrice,
                IsBaseUnit = true
            }, barcode);

            AttachUnits(product);
            return product;
        }

        public Product Update(string id, UpdateProductRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid product data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Product not found");

            existing.Name = request.Name ?? existing.Name;
            existing.BuyPrice = request.BuyPrice;
            existing.StockQuantity = request.StockQuantity;
            existing.Notes = request.Notes ?? existing.Notes;
            if (request.AllowDiscount.HasValue)
                existing.AllowDiscount = request.AllowDiscount.Value;
            if (request.LowStockThreshold.HasValue)
                existing.LowStockThreshold = request.LowStockThreshold.Value;

            // Master-data references: null keeps, empty clears, a value assigns.
            if (request.CategoryId != null)
                existing.CategoryId = ResolveCategoryRef(request.CategoryId, existing.CategoryId);
            if (request.BrandId != null)
                existing.BrandId = ResolveBrandRef(request.BrandId, existing.BrandId);

            if (request.RetailPrice.HasValue && request.RetailPrice.Value < 0)
                throw new DomainValidationException("Retail price cannot be negative");
            if (request.WholesalePrice.HasValue && request.WholesalePrice.Value < 0)
                throw new DomainValidationException("Wholesale price cannot be negative");

            var baseUnit = _unitRepo.GetBaseUnit(id);
            if (baseUnit != null)
            {
                var baseUnitName = baseUnit.UnitName;
                var resolvedUnitId = ResolveUnitRef(request.UnitId, ref baseUnitName);
                if (!string.IsNullOrWhiteSpace(request.UnitName))
                    baseUnit.UnitName = request.UnitName.Trim();
                else if (resolvedUnitId != null)
                    baseUnit.UnitName = baseUnitName;
                baseUnit.UnitId = resolvedUnitId ?? baseUnit.UnitId;
                if (request.RetailPrice.HasValue)
                    baseUnit.RetailPrice = request.RetailPrice.Value;
                if (request.WholesalePrice.HasValue)
                    baseUnit.WholesalePrice = request.WholesalePrice.Value;
            }

            _repo.UpdateWithBaseUnit(existing, baseUnit, request.Barcode?.Trim());

            AttachUnits(existing);
            return existing;
        }

        public void Delete(string id)
        {
            if (!_repo.Delete(id))
                throw new NotFoundException("Product not found");
        }

        public ProductUnit AddUnit(string productId, AddUnitRequest request)
        {
            UnitRules.ValidateNewUnit(request?.UnitName, request?.QuantityFactor ?? 0,
                request?.RetailPrice ?? 0, request?.WholesalePrice);

            var product = _repo.GetById(productId);
            if (product == null)
                throw new NotFoundException("Product not found");

            var unitName = request.UnitName.Trim();
            var unitId = ResolveUnitRef(request.UnitId, ref unitName);

            var unit = _unitRepo.Create(new ProductUnit
            {
                ProductId = productId,
                UnitName = unitName,
                UnitId = unitId,
                QuantityFactor = request.QuantityFactor,
                RetailPrice = request.RetailPrice,
                WholesalePrice = request.WholesalePrice,
                IsBaseUnit = false
            });

            unit.Barcodes = _unitRepo.GetBarcodesByUnit(unit.Id);
            return unit;
        }

        public ProductUnit UpdateUnit(string productId, string unitId, UpdateUnitRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid unit data");

            var unit = _unitRepo.GetById(unitId);
            if (unit == null || unit.ProductId != productId)
                throw new NotFoundException("Unit not found");

            UnitRules.ValidateUnitUpdate(unit, request.UnitName, request.QuantityFactor,
                request.RetailPrice, request.WholesalePrice);

            if (!string.IsNullOrWhiteSpace(request.UnitName))
                unit.UnitName = request.UnitName.Trim();

            if (request.UnitId != null)
            {
                var masterName = unit.UnitName;
                var resolvedUnitId = ResolveUnitRef(request.UnitId, ref masterName);
                if (resolvedUnitId != null)
                {
                    unit.UnitId = resolvedUnitId;
                    if (string.IsNullOrWhiteSpace(request.UnitName))
                        unit.UnitName = masterName;
                }
            }

            if (request.QuantityFactor.HasValue)
                unit.QuantityFactor = request.QuantityFactor.Value;
            if (request.RetailPrice.HasValue)
                unit.RetailPrice = request.RetailPrice.Value;
            if (request.WholesalePrice.HasValue)
                unit.WholesalePrice = request.WholesalePrice.Value;

            _unitRepo.Update(unit);
            return unit;
        }

        public void DeleteUnit(string productId, string unitId)
        {
            var unit = _unitRepo.GetById(unitId);
            if (unit == null || unit.ProductId != productId)
                throw new NotFoundException("Unit not found");
            UnitRules.EnsureNotBaseUnit(unit);

            if (!_unitRepo.Delete(unitId))
                throw new InvalidOperationException("Failed to delete unit");
        }

        public ProductBarcode AddBarcode(string productId, string unitId, AddBarcodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Barcode))
                throw new DomainValidationException("Barcode is required");

            var unit = _unitRepo.GetById(unitId);
            if (unit == null || unit.ProductId != productId)
                throw new NotFoundException("Unit not found");

            var barcode = request.Barcode.Trim();
            if (_unitRepo.BarcodeExists(barcode))
                throw new DomainValidationException("This barcode is already assigned to another product.");

            return _unitRepo.AddBarcode(unitId, barcode);
        }

        public void DeleteBarcode(string productId, string unitId, string barcodeId)
        {
            var row = _unitRepo.GetBarcodeById(barcodeId);
            if (row == null || row.ProductUnitId != unitId)
                throw new NotFoundException("Barcode not found");

            var unit = _unitRepo.GetById(unitId);
            if (unit == null || unit.ProductId != productId)
                throw new NotFoundException("Barcode not found");

            UnitRules.EnsureNotDefaultBarcode(row);

            if (!_unitRepo.DeleteBarcode(barcodeId))
                throw new InvalidOperationException("Failed to delete barcode");
        }

        public void SetDefaultBarcode(string productId, string unitId, string barcodeId)
        {
            var row = _unitRepo.GetBarcodeById(barcodeId);
            if (row == null || row.ProductUnitId != unitId)
                throw new NotFoundException("Barcode not found");

            var unit = _unitRepo.GetById(unitId);
            if (unit == null || unit.ProductId != productId)
                throw new NotFoundException("Barcode not found");

            _unitRepo.SetDefaultBarcode(unitId, barcodeId);
        }

        /// <summary>
        /// Resolves a category reference for a product: null keeps the current value,
        /// empty clears it, and an id must point to an active category (spec §3.3 rule 10).
        /// </summary>
        private string ResolveCategoryRef(string categoryId, string current)
        {
            if (categoryId == null)
                return current;
            var trimmed = categoryId.Trim();
            if (trimmed.Length == 0)
                return null;

            var category = _categoryRepo.GetById(trimmed);
            if (category == null)
                throw new NotFoundException("Category not found");
            if (!category.IsActive)
                throw new DomainValidationException("Inactive categories cannot be assigned to products");
            return category.Id;
        }

        /// <summary>Same reference semantics as categories, for brands.</summary>
        private string ResolveBrandRef(string brandId, string current)
        {
            if (brandId == null)
                return current;
            var trimmed = brandId.Trim();
            if (trimmed.Length == 0)
                return null;

            var brand = _brandRepo.GetById(trimmed);
            if (brand == null)
                throw new NotFoundException("Brand not found");
            if (!brand.IsActive)
                throw new DomainValidationException("Inactive brands cannot be assigned to products");
            return brand.Id;
        }

        /// <summary>
        /// Resolves a shared Unit master reference; on success the master's name becomes the
        /// snapshot UnitName. Returns null when no unitId was supplied (legacy free-text path).
        /// </summary>
        private string ResolveUnitRef(string unitId, ref string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitId))
                return null;

            var master = _masterUnits.GetById(unitId.Trim());
            if (master == null)
                throw new NotFoundException("Unit not found");
            if (!master.IsActive)
                throw new DomainValidationException("Inactive units cannot be assigned to products");

            unitName = master.Name;
            return master.Id;
        }

        private void AttachUnits(IEnumerable<Product> products)
        {
            foreach (var product in products)
            {
                var units = _unitRepo.GetByProduct(product.Id);
                foreach (var unit in units)
                    unit.Barcodes = _unitRepo.GetBarcodesByUnit(unit.Id);

                product.Units = units.ToList();
                product.Barcodes = product.Units.SelectMany(u => u.Barcodes).ToList();
                product.Barcode = product.Barcodes.FirstOrDefault(b => b.IsDefault)?.Barcode
                    ?? product.Barcodes.FirstOrDefault()?.Barcode;
                var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit);
                product.SalePrice = baseUnit?.RetailPrice ?? 0;
            }
        }

        private void AttachUnits(Product product)
        {
            AttachUnits(new[] { product });
        }
    }
}
