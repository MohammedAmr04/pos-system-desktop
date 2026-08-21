namespace PosCs.Application.Ports
{
    /// <summary>Permission/feature checks and tenant resolution for the current user.</summary>
    public interface IAccessControl
    {
        bool HasPermission(string userId, string permissionKey);
        bool HasFeature(string tenantId, string featureKey);
        string GetTenantIdForUser(string userId);
    }

    /// <summary>Clock abstraction so time-dependent rules (throttle, license age) are testable.</summary>
    public interface IClock
    {
        System.DateTime UtcNow { get; }
        /// <summary>Local midnight-based today, matching legacy DateTime.Today usage.</summary>
        System.DateTime Today { get; }
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string passwordHash, string password);
    }

    public interface ITokenService
    {
        string Issue(string secret, string userId);
        /// <summary>Returns the subject (user id) or null when invalid/expired.</summary>
        string Validate(string secret, string token);
    }

    public interface IMachineIdProvider
    {
        string GetMachineId();
    }
}
