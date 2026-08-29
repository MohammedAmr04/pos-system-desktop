using PosCs.Domain.Rules;
using Xunit;

namespace PosCs.Tests.Domain
{
    public class ShiftCashRulesTests
    {
        [Fact]
        public void WalkIn_Cash_Sale_Counts_As_Cash_Sale()
        {
            var r = ShiftCashRules.FromPayment(isSale: true, isPurchase: false, hasClient: false, amount: 1479.0);
            Assert.Equal(ShiftCashRules.Bucket.CashSale, r.Bucket);
            Assert.Equal(1479.0, r.DrawerEffect);
            Assert.Equal(1479.0, r.Delta);
        }

        [Fact]
        public void WalkIn_Sale_Refund_Counts_As_Sale_Refund()
        {
            var r = ShiftCashRules.FromPayment(true, false, false, -51.0);
            Assert.Equal(ShiftCashRules.Bucket.SaleRefund, r.Bucket);
            Assert.Equal(-51.0, r.DrawerEffect);
            Assert.Equal(51.0, r.Delta);
        }

        [Fact]
        public void Registered_Client_Cash_Sale_Counts_As_Cash_Sale()
        {
            var r = ShiftCashRules.FromPayment(true, false, true, 230.0);
            Assert.Equal(ShiftCashRules.Bucket.CashSale, r.Bucket);
            Assert.Equal(230.0, r.DrawerEffect);
            Assert.Equal(230.0, r.Delta);
        }

        [Fact]
        public void Client_Payment_Without_Invoice_Counts_As_Other_Cash_In()
        {
            var r = ShiftCashRules.FromPayment(false, false, true, 40.0);
            Assert.Equal(ShiftCashRules.Bucket.OtherCashIn, r.Bucket);
            Assert.Equal(40.0, r.DrawerEffect);
            Assert.Equal(40.0, r.Delta);
        }

        [Fact]
        public void Supplier_Payment_Without_Invoice_Moves_Money_Out()
        {
            var r = ShiftCashRules.FromPayment(false, false, false, 300.0);
            Assert.Equal(ShiftCashRules.Bucket.SupplierPaymentOut, r.Bucket);
            Assert.Equal(-300.0, r.DrawerEffect);
            Assert.Equal(300.0, r.Delta);
        }

        [Fact]
        public void Purchase_Refund_Counts_As_Refund_In()
        {
            var r = ShiftCashRules.FromPayment(false, true, false, -50.0);
            Assert.Equal(ShiftCashRules.Bucket.SupplierRefundIn, r.Bucket);
            Assert.Equal(50.0, r.DrawerEffect);
            Assert.Equal(50.0, r.Delta);
        }

        [Fact]
        public void Purchase_Linked_Payment_Moves_Money_Out()
        {
            var r = ShiftCashRules.FromPayment(false, true, false, 80.0);
            Assert.Equal(ShiftCashRules.Bucket.SupplierPaymentOut, r.Bucket);
            Assert.Equal(-80.0, r.DrawerEffect);
            Assert.Equal(80.0, r.Delta);
        }
    }
}