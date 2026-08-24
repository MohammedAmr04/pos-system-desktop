using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    /// <summary>A cash shift/session (plan Phase 9): one active shift at a time; drawer
    /// movements are Payment rows stamped with the shift, expected cash is derived.</summary>
    public class Shift
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string OpenedBy { get; set; }
        public double OpeningCash { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        /// <summary>What the cashier physically counted at close (spec §27).</summary>
        public double? CountedCash { get; set; }
        /// <summary>Derived at close from stamped cash payments — never editable.</summary>
        public double? ExpectedCash { get; set; }
        /// <summary>Counted − expected; positive means surplus, negative means shortage.</summary>
        public double? Difference { get; set; }
        public string Notes { get; set; }
        /// <summary>'open' | 'closed'.</summary>
        public string Status { get; set; } = "open";
    }
}
