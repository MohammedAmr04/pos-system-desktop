using System;

namespace PosCs.Domain.Rules
{
    public static class LicenseMachineBinding
    {
        public static bool IsMatch(string storedMachineId, string currentMachineId)
        {
            return !string.IsNullOrWhiteSpace(storedMachineId)
                && !string.IsNullOrWhiteSpace(currentMachineId)
                && string.Equals(storedMachineId.Trim(), currentMachineId.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
