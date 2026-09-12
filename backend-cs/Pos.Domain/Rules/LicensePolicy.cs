using System;
using PosCs.Domain.Exceptions;

namespace PosCs.Domain.Rules
{
    public static class LicensePolicy
    {
        public const string Trial = "trial";
        public const string Monthly = "monthly";
        public const string Annual = "annual";
        public const string Permanent = "permanent";

        public static DateTime CalculateExpiry(string licenseType, DateTime startedAt, int trialDays)
        {
            Validate(licenseType, trialDays, startedAt, null);
            if (string.Equals(licenseType, Permanent, StringComparison.OrdinalIgnoreCase))
                return DateTime.MaxValue;
            if (string.Equals(licenseType, Monthly, StringComparison.OrdinalIgnoreCase))
                return startedAt.Date.AddMonths(1);
            if (string.Equals(licenseType, Annual, StringComparison.OrdinalIgnoreCase))
                return startedAt.Date.AddYears(1);
            return startedAt.Date.AddDays(trialDays);
        }

        public static bool IsExpired(string licenseType, DateTime? expiresAt, DateTime today)
        {
            if (string.Equals(licenseType, Permanent, StringComparison.OrdinalIgnoreCase))
                return false;
            return !expiresAt.HasValue || today.Date >= expiresAt.Value.Date;
        }

        public static void Validate(string licenseType, int trialDays, DateTime? startedAt, DateTime? expiresAt)
        {
            if (!string.Equals(licenseType, Trial, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(licenseType, Monthly, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(licenseType, Annual, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(licenseType, Permanent, StringComparison.OrdinalIgnoreCase))
                throw new DomainValidationException("Invalid license type");
            if (trialDays < 1 || trialDays > 3650)
                throw new DomainValidationException("Trial days must be between 1 and 3650");
            if (!startedAt.HasValue)
                throw new DomainValidationException("License start date is required");
            if (!string.Equals(licenseType, Permanent, StringComparison.OrdinalIgnoreCase)
                && expiresAt.HasValue && expiresAt.Value.Date <= startedAt.Value.Date)
                throw new DomainValidationException("License expiry must be after the start date");
        }
    }
}
