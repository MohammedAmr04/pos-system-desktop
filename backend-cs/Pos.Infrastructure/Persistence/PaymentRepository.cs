using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class PaymentRepository : IPaymentRepository
    {
        public Payment Create(Payment payment)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                // Attribute the movement to the shift that was open when it happened (Phase 9).
                if (string.IsNullOrEmpty(payment.ShiftId))
                    payment.ShiftId = conn.ExecuteScalar<string>(
                        "SELECT id FROM Shift WHERE status = 'open' ORDER BY number DESC LIMIT 1");

                conn.Execute(@"
                    INSERT INTO Payment (id, amount, paymentMethod, date, invoiceId, clientId, supplierId, reference, notes, createdBy, shiftId, createdAt)
                    VALUES (@Id, @Amount, @PaymentMethod, @Date, @InvoiceId, @ClientId, @SupplierId, @Reference, @Notes, @CreatedBy, @ShiftId, @CreatedAt)",
                    new
                    {
                        payment.Id,
                        payment.Amount,
                        payment.PaymentMethod,
                        Date = payment.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        InvoiceId = string.IsNullOrEmpty(payment.InvoiceId) ? null : payment.InvoiceId,
                        ClientId = string.IsNullOrEmpty(payment.ClientId) ? null : payment.ClientId,
                        SupplierId = string.IsNullOrEmpty(payment.SupplierId) ? null : payment.SupplierId,
                        Reference = string.IsNullOrEmpty(payment.Reference) ? null : payment.Reference,
                        Notes = string.IsNullOrEmpty(payment.Notes) ? null : payment.Notes,
                        payment.CreatedBy,
                        ShiftId = string.IsNullOrEmpty(payment.ShiftId) ? null : payment.ShiftId,
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                return payment;
            }
        }

        public PaymentPageResult GetPaged(string clientId, string supplierId, string invoiceId, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    clauses.Add("clientId = @clientId");
                }
                if (!string.IsNullOrWhiteSpace(supplierId))
                {
                    clauses.Add("supplierId = @supplierId");
                }
                if (!string.IsNullOrWhiteSpace(invoiceId))
                {
                    clauses.Add("invoiceId = @invoiceId");
                }

                var parameters = new DynamicParameters();
                parameters.Add("clientId", clientId);
                parameters.Add("supplierId", supplierId);
                parameters.Add("invoiceId", invoiceId);
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Payment{where}", parameters);
                var items = conn.Query<Payment>(
                    $"SELECT * FROM Payment{where} ORDER BY date DESC, rowid DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var p in items)
                    AttachParty(conn, p);

                return new PaymentPageResult { Items = items, Total = total };
            }
        }

        public double SumByInvoice(string invoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.ExecuteScalar<double>(
                    "SELECT COALESCE(SUM(amount), 0) FROM Payment WHERE invoiceId = @invoiceId",
                    new { invoiceId });
            }
        }

        public double SumByClient(string clientId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.ExecuteScalar<double>(
                    "SELECT COALESCE(SUM(amount), 0) FROM Payment WHERE clientId = @clientId",
                    new { clientId });
            }
        }

        public double SumBySupplier(string supplierId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.ExecuteScalar<double>(
                    "SELECT COALESCE(SUM(amount), 0) FROM Payment WHERE supplierId = @supplierId",
                    new { supplierId });
            }
        }

        public List<Payment> ListByClient(string clientId)
        {
            return ListByParty("clientId", clientId);
        }

        public List<Payment> ListBySupplier(string supplierId)
        {
            return ListByParty("supplierId", supplierId);
        }

        public Dictionary<string, double> SumGroupedByInvoice(IEnumerable<string> invoiceIds)
        {
            var ids = (invoiceIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var result = new Dictionary<string, double>();
            if (ids.Count == 0)
                return result;

            using (var conn = DbConnectionFactory.CreateConnection())
            {
                foreach (var group in ids.Select((id, index) => new { id, index }).GroupBy(x => x.index / 500))
                {
                    var batch = group.Select(x => x.id).ToList();
                    var rows = conn.Query<PaidRow>(@"
                        SELECT invoiceId AS InvoiceId, SUM(amount) AS Paid
                        FROM Payment
                        WHERE invoiceId IN @ids
                        GROUP BY invoiceId",
                        new { ids = batch });
                    foreach (var row in rows)
                        result[row.InvoiceId] = row.Paid;
                }
                return result;
            }
        }

        private sealed class PaidRow
        {
            public string InvoiceId { get; set; }
            public double Paid { get; set; }
        }

        private static List<Payment> ListByParty(string partyColumn, string partyId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var items = conn.Query<Payment>(
                    $"SELECT * FROM Payment WHERE {partyColumn} = @partyId ORDER BY date ASC, rowid ASC LIMIT 500",
                    new { partyId }).ToList();
                foreach (var p in items)
                    AttachParty(conn, p);
                return items;
            }
        }

        private static void AttachParty(SqliteConnection conn, Payment payment)
        {
            if (!string.IsNullOrEmpty(payment.InvoiceId))
            {
                var num = conn.ExecuteScalar<string>(
                    "SELECT CAST(invoiceNumber AS TEXT) FROM Invoice WHERE id = @id", new { id = payment.InvoiceId });
                if (num != null)
                    payment.InvoiceNumber = "#" + num;
                else
                {
                    num = conn.ExecuteScalar<string>(
                        "SELECT CAST(invoiceNumber AS TEXT) FROM PurchaseInvoice WHERE id = @id", new { id = payment.InvoiceId });
                    if (num != null)
                        payment.InvoiceNumber = "P-" + num;
                }
            }

            if (!string.IsNullOrEmpty(payment.ClientId))
            {
                payment.Client = conn.QueryFirstOrDefault<Client>(
                    "SELECT * FROM Client WHERE id = @id", new { id = payment.ClientId });
            }
            else if (!string.IsNullOrEmpty(payment.SupplierId))
            {
                payment.Supplier = conn.QueryFirstOrDefault<Supplier>(
                    "SELECT * FROM Supplier WHERE id = @id", new { id = payment.SupplierId });
            }
        }
    }
}
