using System;

namespace PosCs.Domain.Entities
{
    public class Settings
    {
        public string Id { get; set; }
        public string MachineId { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime LastCheckedAt { get; set; }
        public DateTime? LastSeenDate { get; set; }
        public bool Unlocked { get; set; }
        public string LicenseType { get; set; }
        public int TrialDays { get; set; }
        public DateTime? LicenseStartedAt { get; set; }
        public DateTime? LicenseExpiresAt { get; set; }
        public string TokenSecret { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
