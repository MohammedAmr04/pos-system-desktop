using PosCs.Domain.Entities;
using PosCs.Application.Models;

namespace PosCs.Application.Ports
{
    public interface IInvoiceRepository
    {
        Invoice GetById(string id);
        System.Collections.Generic.List<Invoice> GetRange(System.DateTime? from, System.DateTime? to);
        InvoicePageResult GetPaged(System.DateTime? from, System.DateTime? to, string query, int page, int pageSize);
        /// <summary>Persists invoice + lines and decrements stock atomically. Throws InsufficientStockException when stock is missing.</summary>
        Invoice Create(Invoice invoice, System.Collections.Generic.List<InvoiceDetail> lines);
    }
}
