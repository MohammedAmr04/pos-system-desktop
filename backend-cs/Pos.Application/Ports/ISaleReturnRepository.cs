using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    public class SaleReturnPageResult
    {
        public List<SaleReturn> Items { get; set; }
        public int Total { get; set; }
    }

    public interface ISaleReturnRepository
    {
        /// <summary>Runs the return pipeline transactionally: restock via the ledger,
        /// restore consumed FIFO layers (spec §24), auto refund payment for cash sales,
        /// and update the original invoice's returnStatus.</summary>
        SaleReturn Create(SaleReturn saleReturn, List<SaleReturnDetail> details);
        SaleReturnPageResult GetPaged(int page, int pageSize, string invoiceId);
        List<SaleReturnDetail> GetDetails(string returnId);
        /// <summary>Returned quantity per invoice detail id, across all returns of that invoice.</summary>
        Dictionary<string, double> SumReturnedByInvoice(string invoiceId);
    }
}
