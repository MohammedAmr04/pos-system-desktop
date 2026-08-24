using PosCs.Domain.Entities;
using PosCs.Application.Models;

namespace PosCs.Application.Ports
{
    public interface IInvoiceRepository
    {
        Invoice GetById(string id);
        System.Collections.Generic.List<Invoice> GetRange(System.DateTime? from, System.DateTime? to);
        InvoicePageResult GetPaged(System.DateTime? from, System.DateTime? to, string query, string status, int page, int pageSize);
        /// <summary>Persists invoice + lines; 'posted' also runs the full pipeline (stock, FIFO,
        /// auto cash payment). Throws InsufficientStockException when stock is missing.</summary>
        Invoice Create(Invoice invoice, System.Collections.Generic.List<InvoiceDetail> lines);
        /// <summary>Draft-only edit: replaces header + lines with zero side effects.</summary>
        Invoice Update(string id, Invoice invoice, System.Collections.Generic.List<InvoiceDetail> lines);
        /// <summary>Draft -> posted: runs the full pipeline atomically.</summary>
        Invoice Post(string id);
        /// <summary>Posted -> cancelled: safe reversal (stock back, FIFO layers restored).</summary>
        Invoice Cancel(string id);

        /// <summary>Posted sales for one client, oldest first (account statements).</summary>
        System.Collections.Generic.List<Invoice> ListPostedByClient(string clientId);
    }
}
