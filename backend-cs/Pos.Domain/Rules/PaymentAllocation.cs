using System.Collections.Generic;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// One document (invoice/purchase) participating in general-payment allocation.
    /// </summary>
    public sealed class AllocationInput
    {
        public string Id { get; set; }
        public double Total { get; set; }
        public double LinkedPaid { get; set; }
    }

    /// <summary>
    /// Distributes a general (not invoice-linked) payment pool across documents,
    /// oldest first: the earliest document is settled before later ones take a share.
    /// Display-only — it never changes stored payments; statement balances are
    /// unaffected. A negative pool is treated as zero.
    /// </summary>
    public static class PaymentAllocation
    {
        public static Dictionary<string, double> Allocate(IEnumerable<AllocationInput> documentsOldestFirst, double generalPool)
        {
            var result = new Dictionary<string, double>();
            var pool = generalPool > 0 ? generalPool : 0;
            foreach (var doc in documentsOldestFirst)
            {
                var take = 0.0;
                var need = doc.Total - doc.LinkedPaid;
                if (need > 0 && pool > 0)
                {
                    take = need < pool ? need : pool;
                    pool -= take;
                }
                result[doc.Id] = doc.LinkedPaid + take;
            }
            return result;
        }
    }
}
