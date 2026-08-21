using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>The requested entity does not exist (maps to HTTP 404).</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
