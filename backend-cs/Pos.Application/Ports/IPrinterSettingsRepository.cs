using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Key/value persistence for printer & receipt settings (plan Phase 12).
    /// Get() returns code defaults when nothing is stored yet.</summary>
    public interface IPrinterSettingsRepository
    {
        PrinterSettings Get();

        void Save(PrinterSettings settings);
    }
}
