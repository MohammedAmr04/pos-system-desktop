using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    public class PrintQueueService
    {
        private readonly IPrintQueueRepository _repository;

        public PrintQueueService(IPrintQueueRepository repository)
        {
            _repository = repository;
        }

        public void Enqueue(string jobType, string payload)
        {
            if (string.IsNullOrWhiteSpace(jobType) || string.IsNullOrWhiteSpace(payload))
                throw new DomainValidationException("Print job is invalid");
            _repository.Enqueue(jobType.Trim(), payload);
        }

        public List<PrintJob> GetPending(int limit)
        {
            if (limit < 1) limit = 1;
            if (limit > 50) limit = 50;
            return _repository.GetPending(limit);
        }

        public void MarkAttempt(string id, string error) { _repository.MarkAttempt(id, error); }
        public void MarkPrinted(string id) { _repository.MarkPrinted(id); }
        public PrintQueueStatus GetStatus() { return _repository.GetStatus(); }
    }
}
