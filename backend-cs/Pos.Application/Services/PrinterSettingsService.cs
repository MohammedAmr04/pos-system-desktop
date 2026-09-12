using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Printer & receipt settings use cases (plan Phase 12): read stored config
    /// (code defaults when unset) and persist a normalized copy.</summary>
    public class PrinterSettingsService
    {
        private readonly IPrinterSettingsRepository _repository;

        public PrinterSettingsService(IPrinterSettingsRepository repository)
        {
            _repository = repository;
        }

        public PrinterSettings Get()
        {
            return _repository.Get();
        }

        public PrinterSettings Save(PrinterSettings settings)
        {
            if (settings == null)
                throw new DomainValidationException("Invalid printer settings");
            if (string.IsNullOrWhiteSpace(settings.ReceiptPrinterName))
                throw new DomainValidationException("Receipt printer name is required");
            if (string.IsNullOrWhiteSpace(settings.LabelPrinterName))
                throw new DomainValidationException("Label printer name is required");
            if (settings.PaperWidthMm != 58 && settings.PaperWidthMm != 80)
                throw new DomainValidationException("Paper width must be 58 or 80 mm");

            _repository.Save(settings);
            return _repository.Get();
        }
    }
}
