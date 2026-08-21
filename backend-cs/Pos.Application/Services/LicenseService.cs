using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>License status and unlock use cases.</summary>
    public class LicenseService
    {
        private readonly ILicenseRepository _settings;
        private readonly IMachineIdProvider _machineId;
        private readonly IClock _clock;

        public LicenseService(ILicenseRepository settings, IMachineIdProvider machineId, IClock clock)
        {
            _settings = settings;
            _machineId = machineId;
            _clock = clock;
        }

        public LicenseStatus GetStatus()
        {
            var machineId = _machineId.GetMachineId();

            var settings = _settings.GetByMachineId(machineId);
            if (settings == null)
            {
                _settings.Create(machineId, false);
                return new LicenseStatus { Status = "first_boot", MachineId = machineId };
            }

            if (!settings.Unlocked)
                return new LicenseStatus { Status = "locked", MachineId = machineId };

            return new LicenseStatus
            {
                Status = "ok",
                MachineId = machineId,
                DaysSinceActivation = (int)(_clock.UtcNow - settings.ActivatedAt).TotalDays
            };
        }

        public string Unlock(string machineId, string code)
        {
            if (machineId == null)
                throw new DomainValidationException("Machine ID required");

            if (!LicenseCode.IsValidUnlockCode(machineId, code))
                throw new DomainValidationException("Invalid unlock code");

            _settings.Upsert(machineId, true);
            return machineId;
        }
    }
}
