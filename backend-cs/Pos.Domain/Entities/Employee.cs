using System;

namespace PosCs.Domain.Entities
{
    /// <summary>Lightweight staff directory for sales attribution. Employees have no
    /// login — system access stays on User accounts; invoices optionally link here.</summary>
    public class Employee
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
