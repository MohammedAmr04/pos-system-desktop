using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Per-operation access to product units and their barcodes.</summary>
    public interface IProductUnitRepository
    {
        System.Collections.Generic.List<ProductUnit> GetByProduct(string productId);
        ProductUnit GetById(string unitId);
        ProductUnit GetBaseUnit(string productId);
        System.Collections.Generic.List<ProductBarcode> GetBarcodesByUnit(string unitId);
        ProductBarcode GetBarcodeById(string barcodeId);
        bool BarcodeExists(string barcode);
        /// <summary>Generates a random 12-digit barcode that is not yet in use.</summary>
        string GenerateUniqueBarcode();
        ProductUnit Create(ProductUnit unit);
        void Update(ProductUnit unit);
        bool Delete(string unitId);
        ProductBarcode AddBarcode(string unitId, string barcode, bool isDefault = false);
        bool DeleteBarcode(string barcodeId);
        bool SetDefaultBarcode(string unitId, string barcodeId);
    }
}
