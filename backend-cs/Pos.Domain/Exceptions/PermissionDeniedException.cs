using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>The authenticated user lacks the required permission (maps to HTTP 403).</summary>
    public class PermissionDeniedException : Exception
    {
        public PermissionDeniedException(string permissionKey)
            : base($"Permission denied: {permissionKey}") { }
    }
}
