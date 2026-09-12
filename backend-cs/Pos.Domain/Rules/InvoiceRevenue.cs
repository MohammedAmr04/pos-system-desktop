namespace PosCs.Domain.Rules
{
    public static class InvoiceRevenue
    {
        public static double Net(double postedInvoiceTotal, double postedReturnTotal)
        {
            return InvoicePricing.Round2(postedInvoiceTotal - postedReturnTotal);
        }
    }
}
