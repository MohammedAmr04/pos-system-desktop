using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>Stock on hand cannot cover the requested quantity (maps to HTTP 400).</summary>
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string message) : base(message) { }
    }
}
