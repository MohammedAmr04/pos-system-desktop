using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using Xunit;

namespace PosCs.Tests.Application
{
    public class PrintQueueServiceTests
    {
        [Fact]
        public void PendingLimitIsClamped()
        {
            var repository = new FakePrintQueueRepository();
            var service = new PrintQueueService(repository);

            service.GetPending(500);

            Assert.Equal(50, repository.LastLimit);
        }

        [Fact]
        public void EmptyJobIsRejected()
        {
            var service = new PrintQueueService(new FakePrintQueueRepository());

            Assert.Throws<PosCs.Domain.Exceptions.DomainValidationException>(() => service.Enqueue("receipt", ""));
        }

        private sealed class FakePrintQueueRepository : IPrintQueueRepository
        {
            public int LastLimit { get; private set; }
            public void Enqueue(string jobType, string payload) { }
            public List<PrintJob> GetPending(int limit) { LastLimit = limit; return new List<PrintJob>(); }
            public void MarkAttempt(string id, string error) { }
            public void MarkPrinted(string id) { }
            public PrintQueueStatus GetStatus() { return new PrintQueueStatus(); }
        }
    }
}
