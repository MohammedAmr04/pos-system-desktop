using System;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Shift use cases (plan Phase 9, spec §27): one open shift at a time; expected
    /// cash is derived from stamped cash payments at close — the cashier only reports a count.</summary>
    public class ShiftService
    {
        private readonly IShiftRepository _shifts;

        public ShiftService(IShiftRepository shifts)
        {
            _shifts = shifts;
        }

        public Shift Open(OpenShiftRequest request, string userId)
        {
            if (request == null)
                throw new DomainValidationException("Invalid shift data");
            if (request.OpeningCash < 0)
                throw new DomainValidationException("Opening cash cannot be negative");

            return _shifts.Create(new Shift
            {
                OpenedBy = userId,
                OpeningCash = Math.Round(request.OpeningCash, 2),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            });
        }

        public Shift Close(string id, CloseShiftRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid shift data");
            if (request.CountedCash < 0)
                throw new DomainValidationException("Counted cash cannot be negative");

            return _shifts.Close(id, request.CountedCash);
        }

        public Shift GetActive()
        {
            return _shifts.GetActive();
        }

        public Shift GetById(string id)
        {
            return _shifts.GetById(id);
        }

        public ShiftPageResult GetPaged(string status, int page, int pageSize)
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;
            return _shifts.GetPaged(status, page, pageSize);
        }

        public ShiftReport GetReport(string id)
        {
            return _shifts.GetReport(id);
        }

        public PagedResult<Invoice> GetShiftInvoices(string id, int page, int pageSize)
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;
            return _shifts.GetShiftInvoices(id, page, pageSize);
        }
    }
}
