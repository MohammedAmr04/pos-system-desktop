using System;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>
    /// The single entry point for inventory mutations (plan Phase 3): every stock change —
    /// sale, purchase, reversal — writes a StockMovement ledger row AND applies the signed
    /// delta to Product.stockQuantity through this writer, inside the caller's transaction.
    /// </summary>
    public static class StockLedger
    {
        public const string Purchase = "purchase";
        public const string PurchaseReversal = "purchase_reversal";
        public const string Sale = "sale";
        public const string SaleReversal = "sale_reversal";
        public const string SaleReturn = "sale_return";
        public const string PurchaseReturn = "purchase_return";
        public const string AdjustmentIn = "adjustment_in";
        public const string AdjustmentOut = "adjustment_out";

        /// <summary>Appends a ledger row and applies the signed base-unit delta to stock.
        /// When requireStock is true a negative resulting stock aborts via InsufficientStockException.</summary>
        public static void Apply(
            SqliteConnection conn,
            SqliteTransaction tx,
            string productId,
            double baseQuantity,
            string type,
            string referenceId,
            string referenceNumber,
            bool requireStock)
        {
            conn.Execute(@"
                INSERT INTO StockMovement (id, productId, quantity, type, referenceId, referenceNumber, createdAt)
                VALUES (@id, @productId, @quantity, @type, @referenceId, @referenceNumber, @createdAt)",
                new
                {
                    id = Guid.NewGuid().ToString("N"),
                    productId,
                    quantity = baseQuantity,
                    type,
                    referenceId,
                    referenceNumber,
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, transaction: tx);

            var sql = requireStock
                ? "UPDATE Product SET stockQuantity = stockQuantity + @delta WHERE id = @productId AND stockQuantity + @delta >= 0"
                : "UPDATE Product SET stockQuantity = stockQuantity + @delta WHERE id = @productId";

            var affected = conn.Execute(sql, new { delta = baseQuantity, productId }, transaction: tx);
            if (affected == 0)
                throw new InsufficientStockException($"Insufficient stock for product '{productId}'");
        }
    }
}
