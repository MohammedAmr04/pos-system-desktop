using System;

namespace PosCs.Domain.Entities
{
    public class CashDrawerMovement
    {
        public string Id { get; set; }
        public string ShiftId { get; set; }
        public string Type { get; set; }
        public double Amount { get; set; }
        public string Reason { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
