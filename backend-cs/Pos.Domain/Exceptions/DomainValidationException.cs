using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>
    /// A business rule was violated. The API layer maps this to HTTP 400 with the
    /// exception message, preserving the legacy error responses.
    /// </summary>
    public class DomainValidationException : Exception
    {
        public DomainValidationException(string message) : base(message) { }
    }
}
