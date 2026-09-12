namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Drawer classification for shift cash rows. A row is attributed to its business
    /// transaction (sale / purchase / client payment / supplier payment) by the invoice it
    /// links to, NOT by Payment.clientId being set — walk-in sales store clientId = NULL.
    /// </summary>
    public static class ShiftCashRules
    {
        /// <summary>Which drawer summary bucket a payment row increments.</summary>
        public enum Bucket
        {
            CashSale,
            SaleRefund,
            OtherCashIn,
            SupplierRefundIn,
            SupplierPaymentOut
        }

        public sealed class Classified
        {
            public Bucket Bucket { get; set; }
            /// <summary>Signed drawer effect for the movement list: positive = money in, negative = money out.</summary>
            public double DrawerEffect { get; set; }
            /// <summary>Positive delta to add to the owning summary bucket.</summary>
            public double Delta { get; set; }
        }

        public static Classified FromPayment(bool isSale, bool isPurchase, bool hasClient, double amount)
        {
            if (isSale)
            {
                if (amount >= 0)
                    return new Classified { Bucket = Bucket.CashSale, DrawerEffect = amount, Delta = amount };
                return new Classified { Bucket = Bucket.SaleRefund, DrawerEffect = amount, Delta = -amount };
            }

            if (isPurchase)
            {
                // Purchase-linked rows: positive = payment out (money leaves the drawer),
                // negative = refund in (money returns to the drawer).
                if (amount >= 0)
                    return new Classified { Bucket = Bucket.SupplierPaymentOut, DrawerEffect = -amount, Delta = amount };
                return new Classified { Bucket = Bucket.SupplierRefundIn, DrawerEffect = -amount, Delta = -amount };
            }

            if (hasClient)
                return new Classified { Bucket = Bucket.OtherCashIn, DrawerEffect = amount, Delta = amount };

            // Unlinked manual supplier payments are stored positive but move money OUT.
            return new Classified { Bucket = Bucket.SupplierPaymentOut, DrawerEffect = -amount, Delta = amount };
        }
    }
}