using System;

namespace PosCs.Domain.Entities
{
    public class Settings
    {
        public string Id { get; set; }
        public string MachineId { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime LastCheckedAt { get; set; }
        public bool Unlocked { get; set; }
        public string TokenSecret { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
