using System;

namespace PosCs.Infrastructure.SystemTime
{
    public class SystemClock : PosCs.Application.Ports.IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Today => DateTime.Today;
    }
}
