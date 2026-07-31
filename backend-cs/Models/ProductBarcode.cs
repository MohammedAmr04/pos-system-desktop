using System;

namespace PosCs.Models
{
    public class ProductBarcode
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string Barcode { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
