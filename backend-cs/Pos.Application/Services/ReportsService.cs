using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>Reporting use cases.</summary>
    public class ReportsService
    {
        private readonly IProductRepository _products;

        public ReportsService(IProductRepository products)
        {
            _products = products;
        }

        public List<Product> LowStock()
        {
            return _products.GetAll()
                .Where(LowStockPolicy.IsLowStock)
                .OrderBy(p => p.StockQuantity)
                .ToList();
        }
    }
}
