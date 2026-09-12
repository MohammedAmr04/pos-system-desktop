using System;

namespace PosCs.Domain.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
