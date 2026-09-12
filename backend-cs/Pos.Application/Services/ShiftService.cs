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

        public Shift Close(string id, CloseShiftRequest request, string userId)
        {
            if (request == null)
                throw new DomainValidationException("Invalid shift data");
            if (request.CountedCash < 0)
                throw new DomainValidationException("Counted cash cannot be negative");

            return _shifts.Close(id, request.CountedCash, userId);
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

        public CashDrawerMovement CreateDrawerMovement(string shiftId, CreateCashDrawerMovementRequest request, string userId)
        {
            if (request == null || (request.Type != "paid_in" && request.Type != "paid_out"))
                throw new DomainValidationException("Invalid cash drawer movement");
            if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Reason))
                throw new DomainValidationException("Amount and reason are required");
            return _shifts.CreateDrawerMovement(new CashDrawerMovement
            {
                ShiftId = shiftId,
                Type = request.Type,
                Amount = Math.Round(request.Amount, 2),
                Reason = request.Reason.Trim(),
                CreatedBy = userId
            });
        }
    }
}
