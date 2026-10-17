using System;
using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public sealed class PrintJob
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string TerminalId { get; set; }
        public string JobType { get; set; }
        public string Payload { get; set; }
        public string Status { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PrintedAt { get; set; }
    }

    public sealed class PrintQueueStatus
    {
        public int Pending { get; set; }
        public int Failed { get; set; }
    }
}
