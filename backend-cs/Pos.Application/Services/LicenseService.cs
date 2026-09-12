using System;
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
            var today = _clock.Today.Date;

            var settings = _settings.GetByMachineId(machineId);
            if (settings == null)
            {
                var existingSettings = _settings.GetFirst();
                if (existingSettings != null && !LicenseMachineBinding.IsMatch(existingSettings.MachineId, machineId))
                    return new LicenseStatus { Status = "tampered", MachineId = machineId };

                _settings.Create(machineId, false);
                _settings.RecordLastSeenDate(machineId, today);
                return new LicenseStatus
                {
                    Status = "trial",
                    MachineId = machineId,
                    DaysSinceActivation = 0,
                    LicenseType = LicensePolicy.Trial,
                    TrialDays = 14,
                    LicenseStartedAt = today,
                    LicenseExpiresAt = today.AddDays(14),
                    RemainingDays = 14
                };
            }

            if (LicenseTimeGuard.IsClockRollback(settings.LastSeenDate, today))
                return new LicenseStatus { Status = "tampered", MachineId = machineId };

            _settings.RecordLastSeenDate(machineId, today);

            var licenseType = string.IsNullOrWhiteSpace(settings.LicenseType)
                ? (settings.Unlocked ? LicensePolicy.Permanent : LicensePolicy.Trial)
                : settings.LicenseType;
            var startedAt = settings.LicenseStartedAt ?? settings.ActivatedAt.Date;
            var expiresAt = settings.LicenseExpiresAt;
            if (!expiresAt.HasValue && !string.Equals(licenseType, LicensePolicy.Permanent, System.StringComparison.OrdinalIgnoreCase))
                expiresAt = LicensePolicy.CalculateExpiry(licenseType, startedAt, settings.TrialDays < 1 ? 14 : settings.TrialDays);
            var trialDays = settings.TrialDays < 1 ? 14 : settings.TrialDays;
            var expired = LicensePolicy.IsExpired(licenseType, expiresAt, today);

            if (!settings.Unlocked && string.Equals(licenseType, LicensePolicy.Permanent, System.StringComparison.OrdinalIgnoreCase))
                return new LicenseStatus { Status = "locked", MachineId = machineId };

            return new LicenseStatus
            {
                Status = expired ? "locked" : (string.Equals(licenseType, LicensePolicy.Trial, System.StringComparison.OrdinalIgnoreCase) ? "trial" : "ok"),
                MachineId = machineId,
                DaysSinceActivation = (int)(_clock.UtcNow - settings.ActivatedAt).TotalDays,
                LicenseType = licenseType,
                TrialDays = trialDays,
                LicenseStartedAt = startedAt,
                LicenseExpiresAt = expiresAt == DateTime.MaxValue ? (System.DateTime?)null : expiresAt,
                RemainingDays = expiresAt.HasValue && expiresAt.Value != DateTime.MaxValue
                    ? (int)(expiresAt.Value.Date - today).TotalDays
                    : (int?)null
            };
        }

        public LicenseStatus SaveConfiguration(LicenseConfigurationRequest request)
        {
            if (request == null)
                throw new DomainValidationException("License configuration is required");

            var type = (request.LicenseType ?? "").Trim().ToLowerInvariant();
            var startedAt = request.LicenseStartedAt?.Date ?? _clock.Today.Date;
            var trialDays = request.TrialDays < 1 ? 14 : request.TrialDays;
            var calculatedExpiry = LicensePolicy.CalculateExpiry(type, startedAt, trialDays);
            var expiresAt = type == LicensePolicy.Permanent ? (DateTime?)null : calculatedExpiry;
            LicensePolicy.Validate(type, trialDays, startedAt, expiresAt);

            var machineId = _machineId.GetMachineId();
            var settings = _settings.GetByMachineId(machineId);
            if (settings == null)
                _settings.Create(machineId, false);
            _settings.SaveLicenseConfiguration(machineId, type, trialDays, startedAt, expiresAt);
            return GetStatus();
        }

        public string Unlock(string machineId, string code)
        {
            if (machineId == null)
                throw new DomainValidationException("Machine ID required");

            var currentMachineId = _machineId.GetMachineId();
            if (!LicenseMachineBinding.IsMatch(currentMachineId, machineId))
                throw new DomainValidationException("Machine ID does not match this device");

            var settings = _settings.GetFirst();
            if (settings != null && !LicenseMachineBinding.IsMatch(settings.MachineId, currentMachineId))
                throw new DomainValidationException("This license belongs to another device");

            if (!LicenseCode.IsValidUnlockCode(machineId, code))
                throw new DomainValidationException("Invalid unlock code");

            _settings.Upsert(machineId, true);
            return machineId;
        }
    }
}
