using System;

namespace PosCs.Domain.Entities
{
    public class Brand
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
