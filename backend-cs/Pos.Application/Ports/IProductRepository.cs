using PosCs.Domain.Entities;
using System.Collections.Generic;

namespace PosCs.Application.Ports
{
    /// <summary>Read/write access to products, including atomic product+base-unit creation.</summary>
    public interface IProductRepository
    {
        Product GetById(string id);
        System.Collections.Generic.List<Product> GetAll();
        System.Collections.Generic.List<Product> GetForPOS();
        System.Collections.Generic.List<Product> Search(string query, int limit);
        Models.PagedResult<Product> GetPaged(int page, int pageSize, string query);
        int Count();
        /// <summary>Creates the product together with its base unit and initial barcode in one transaction.</summary>
        Product CreateWithBaseUnit(Product product, ProductUnit baseUnit, string barcode);
        /// <summary>Updates the product, its base unit (when provided) and optionally renames/adds the default barcode in one transaction.</summary>
        void UpdateWithBaseUnit(Product product, ProductUnit baseUnit, string newDefaultBarcode);
        bool Delete(string id);
    }

    public interface IBundleRepository
    {
        List<BundleComponent> GetBundleComponents(string bundleProductId);
        void ReplaceBundleComponents(string bundleProductId, List<BundleComponent> components);
    }
}
