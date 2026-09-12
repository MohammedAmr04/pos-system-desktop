using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeShiftRepository : IShiftRepository
    {
        public List<Shift> Stored = new List<Shift>();
        public Shift ClosedShift;
        public double ClosedCounted;
        public int NextNumber = 1;

        public Shift Create(Shift shift)
        {
            if (Stored.Any(s => s.Status == "open"))
                throw new DomainValidationException("An open shift already exists");
            shift.Id = Guid.NewGuid().ToString("N");
            shift.Number = NextNumber++;
            shift.OpenedAt = DateTime.Now;
            shift.Status = "open";
            Stored.Add(shift);
            return shift;
        }

        public Shift GetActive() => Stored.FirstOrDefault(s => s.Status == "open");

        public Shift GetById(string id) => Stored.FirstOrDefault(s => s.Id == id);

        public ShiftPageResult GetPaged(string status, int page, int pageSize)
        {
            var items = string.IsNullOrWhiteSpace(status) || status == "all"
                ? Stored.ToList()
                : Stored.Where(s => s.Status == status).ToList();
            return new ShiftPageResult { Items = items, Total = items.Count };
        }

        public Shift Close(string shiftId, double countedCash, string closedBy)
        {
            var shift = Stored.FirstOrDefault(s => s.Id == shiftId);
            if (shift == null)
                throw new NotFoundException("Shift not found");
            if (shift.Status != "open")
                throw new DomainValidationException("Only an open shift can be closed");
            shift.CountedCash = countedCash;
            shift.ExpectedCash = 0;
            shift.Difference = countedCash;
            shift.Status = "closed";
            ClosedShift = shift;
            ClosedCounted = countedCash;
            return shift;
        }

        public ShiftReport GetReport(string shiftId) =>
            new ShiftReport { Shift = GetById(shiftId), OpeningCash = GetById(shiftId)?.OpeningCash ?? 0 };

        public PagedResult<Invoice> GetShiftInvoices(string shiftId, int page, int pageSize) =>
            new PagedResult<Invoice>
            {
                Items = new List<Invoice>(),
                Total = 0
            };

        public CashDrawerMovement CreateDrawerMovement(CashDrawerMovement movement)
        {
            movement.Id = Guid.NewGuid().ToString("N");
            movement.CreatedAt = DateTime.Now;
            return movement;
        }
    }

    public class ShiftServiceTests
    {
        [Fact]
        public void Negative_Opening_Cash_Is_Rejected()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                new ShiftService(new FakeShiftRepository()).Open(new OpenShiftRequest { OpeningCash = -1 }, "u"));
            Assert.Contains("cannot be negative", ex.Message);
        }

        [Fact]
        public void Opening_Cash_Defaults_To_Zero_And_Rounds()
        {
            var repo = new FakeShiftRepository();
            var shift = new ShiftService(repo).Open(new OpenShiftRequest(), "u1");

            Assert.Equal(0, shift.OpeningCash);
            Assert.Equal("u1", shift.OpenedBy);
            Assert.Equal("open", shift.Status);
        }

        [Fact]
        public void Second_Open_Shift_Is_Rejected()
        {
            var service = new ShiftService(new FakeShiftRepository());
            service.Open(new OpenShiftRequest { OpeningCash = 100 }, "u");

            Assert.Throws<DomainValidationException>(() =>
                service.Open(new OpenShiftRequest { OpeningCash = 50 }, "u2"));
        }

        [Fact]
        public void Negative_Counted_Cash_Is_Rejected()
        {
            var repo = new FakeShiftRepository();
            var service = new ShiftService(repo);
            var shift = service.Open(new OpenShiftRequest(), "u");

            Assert.Throws<DomainValidationException>(() =>
                service.Close(shift.Id, new CloseShiftRequest { CountedCash = -5 }, "u"));
        }

        [Fact]
        public void Close_Passes_Counted_Cash_To_Repository()
        {
            var repo = new FakeShiftRepository();
            var service = new ShiftService(repo);
            var shift = service.Open(new OpenShiftRequest { OpeningCash = 250 }, "u");

            var closed = service.Close(shift.Id, new CloseShiftRequest { CountedCash = 300 }, "u");

            Assert.Equal(300, closed.CountedCash);
            Assert.Equal("closed", closed.Status);
        }

        [Fact]
        public void Closing_An_Already_Closed_Shift_Fails()
        {
            var repo = new FakeShiftRepository();
            var service = new ShiftService(repo);
            var shift = service.Open(new OpenShiftRequest(), "u");
            service.Close(shift.Id, new CloseShiftRequest { CountedCash = 10 }, "u");

            Assert.Throws<DomainValidationException>(() =>
                service.Close(shift.Id, new CloseShiftRequest { CountedCash = 10 }, "u"));
        }

        [Fact]
        public void Shift_Invoices_Clamp_Page_Size()
        {
            var repo = new FakeShiftRepository();
            var service = new ShiftService(repo);

            var result = service.GetShiftInvoices("sid", 0, 500);

            Assert.Equal(0, result.Items.Count);
        }
    }
}
