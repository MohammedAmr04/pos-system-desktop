using System;

namespace PosCs.Domain.Rules
{
    public static class LicenseTimeGuard
    {
        public static bool IsClockRollback(DateTime? lastSeenDate, DateTime currentDate)
        {
            return lastSeenDate.HasValue && currentDate.Date < lastSeenDate.Value.Date;
        }
    }
}
