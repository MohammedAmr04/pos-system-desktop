using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public sealed class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Total { get; set; }
    }

    public sealed class InvoicePageResult
    {
        public List<Domain.Entities.Invoice> Items { get; set; } = new List<Domain.Entities.Invoice>();
        public int Total { get; set; }
        public double Revenue { get; set; }
        public double Discounts { get; set; }
        /// <summary>Invoice-linked payment totals by invoice id (payment-status badges).</summary>
        public Dictionary<string, double> PaidByInvoice { get; set; } = new Dictionary<string, double>();
    }

    public sealed class AccessBundle
    {
        public Domain.Entities.User User { get; set; }
        public string TenantId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> Features { get; set; } = new List<string>();
    }

    public sealed class LoginResult
    {
        public AccessBundle Bundle { get; set; }
        public string Token { get; set; }
    }

    public sealed class RoleSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    public sealed class UserSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public bool IsActive { get; set; }
        public List<string> RoleIds { get; set; } = new List<string>();
        public System.DateTime? CreatedAt { get; set; }
    }

    public sealed class FeatureToggleState
    {
        public string Key { get; set; }
        public bool Enabled { get; set; }
    }

    public sealed class LicenseStatus
    {
        public string Status { get; set; }
        public string MachineId { get; set; }
        public int? DaysSinceActivation { get; set; }
    }
}
