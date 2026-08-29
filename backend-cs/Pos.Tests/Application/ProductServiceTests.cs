using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeProductRepository : IProductRepository
    {
        public Product Stored;
        public Product LastCreated;
        public Product LastUpdated;
        public ProductUnit LastCreatedUnit;
        public ProductUnit LastUpdatedUnit;

        public Product GetById(string id)
        {
            return Stored != null && Stored.Id == id ? Stored : null;
        }

        public List<Product> GetAll() => Stored == null ? new List<Product>() : new List<Product> { Stored };

        public List<Product> GetForPOS() => new List<Product>();

        public List<Product> Search(string query, int limit) => new List<Product>();

        public PagedResult<Product> GetPaged(int page, int pageSize, string query) =>
            new PagedResult<Product> { Items = new List<Product>(), Total = 0 };

        public int Count() => 0;

        public Product CreateWithBaseUnit(Product product, ProductUnit baseUnit, string barcode)
        {
            product.Id = "prod-1";
            LastCreated = product;
            LastCreatedUnit = baseUnit;
            Stored = product;
            return product;
        }

        public void UpdateWithBaseUnit(Product product, ProductUnit baseUnit, string newDefaultBarcode)
        {
            LastUpdated = product;
            LastUpdatedUnit = baseUnit;
            Stored = product;
        }

        public bool Delete(string id) => true;
    }

    public class FakeProductUnitRepository : IProductUnitRepository
    {
        public ProductUnit BaseUnit;

        public List<ProductUnit> GetByProduct(string productId) => new List<ProductUnit>();

        public ProductUnit GetById(string unitId) => null;

        public ProductUnit GetBaseUnit(string productId) => BaseUnit;

        public List<ProductBarcode> GetBarcodesByUnit(string unitId) => BaseUnit?.Barcodes ?? new List<ProductBarcode>();

        public ProductBarcode GetBarcodeById(string barcodeId) => null;

        public bool BarcodeExists(string barcode) => false;

        public string GenerateUniqueBarcode() => "111111111111";

        public ProductUnit Create(ProductUnit unit) => unit;

        public void Update(ProductUnit unit) { }

        public bool Delete(string unitId) => true;

        public ProductBarcode AddBarcode(string unitId, string barcode, bool isDefault = false) => null;

        public bool DeleteBarcode(string barcodeId) => true;

        public bool SetDefaultBarcode(string unitId, string barcodeId) => true;

        public void SyncUnitName(string unitId, string newName) { }
    }

    public class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> GetAll() => new List<Category>();
        public PagedResult<Category> GetPaged(int page, int pageSize, string query) => new PagedResult<Category> { Items = new List<Category>(), Total = 0 };
        public Category GetById(string id) => null;
        public Category GetByName(string name) => null;
        public Category Create(Category category) => category;
        public Category Update(Category category) => category;
        public int CountProducts(string categoryId) => 0;
    }

    public class FakeBrandRepository : IBrandRepository
    {
        public List<Brand> GetAll() => new List<Brand>();
        public PagedResult<Brand> GetPaged(int page, int pageSize, string query) => new PagedResult<Brand> { Items = new List<Brand>(), Total = 0 };
        public Brand GetById(string id) => null;
        public Brand GetByName(string name) => null;
        public Brand Create(Brand brand) => brand;
        public Brand Update(Brand brand) => brand;
        public bool Delete(string id) => true;
        public int CountProducts(string brandId) => 0;
    }

    public class FakeUnitRepository : IUnitRepository
    {
        public List<Unit> GetAll() => new List<Unit>();
        public PagedResult<Unit> GetPaged(int page, int pageSize, string query) => new PagedResult<Unit> { Items = new List<Unit>(), Total = 0 };
        public Unit GetById(string id) => null;
        public Unit GetByName(string name) => null;
        public Unit Create(Unit unit) => unit;
        public Unit Update(Unit unit) => unit;
        public bool Delete(string id) => true;
        public int CountProductUnits(string unitId) => 0;
    }

    public class ProductServiceTests
    {
        private static ProductService ServiceWith(
            FakeProductRepository products,
            FakeProductUnitRepository units,
            FakeCategoryRepository categories = null,
            FakeBrandRepository brands = null,
            FakeUnitRepository masterUnits = null)
        {
            return new ProductService(
                products,
                units,
                categories ?? new FakeCategoryRepository(),
                brands ?? new FakeBrandRepository(),
                masterUnits ?? new FakeUnitRepository());
        }

        private static CreateProductRequest CreateRequest()
        {
            return new CreateProductRequest
            {
                Name = "عصير برتقال",
                RetailPrice = 15,
                UnitName = "قطعة",
                BuyPrice = 55,
                StockQuantity = 42
            };
        }

        private static UpdateProductRequest UpdateRequest()
        {
            return new UpdateProductRequest
            {
                Name = "عصير برتقال",
                BuyPrice = 77,
                StockQuantity = 33,
                RetailPrice = 15
            };
        }

        [Fact]
        public void Create_ForcesBuyPriceAndStockToZero()
        {
            var products = new FakeProductRepository();
            var units = new FakeProductUnitRepository();

            var created = ServiceWith(products, units).Create(CreateRequest());

            Assert.Equal(0, created.BuyPrice);
            Assert.Equal(0, created.StockQuantity);
            Assert.Equal(0, products.LastCreated.BuyPrice);
            Assert.Equal(0, products.LastCreated.StockQuantity);
        }

        [Fact]
        public void Update_IgnoresRequestedBuyPriceAndStock()
        {
            var products = new FakeProductRepository();
            var units = new FakeProductUnitRepository
            {
                BaseUnit = new ProductUnit { Id = "u1", UnitName = "قطعة", RetailPrice = 15, IsBaseUnit = true }
            };
            products.Stored = new Product { Id = "prod-1", Name = "عصير برتقال", BuyPrice = 11, StockQuantity = 5 };

            var updated = ServiceWith(products, units).Update("prod-1", UpdateRequest());

            Assert.Equal(11, updated.BuyPrice);
            Assert.Equal(5, updated.StockQuantity);
            Assert.Equal(11, products.LastUpdated.BuyPrice);
            Assert.Equal(5, products.LastUpdated.StockQuantity);
        }
    }
}