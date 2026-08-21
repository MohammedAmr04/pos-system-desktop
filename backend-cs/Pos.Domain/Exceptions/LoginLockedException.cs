using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>Login throttling locked the account temporarily (maps to HTTP 429).</summary>
    public class LoginLockedException : Exception
    {
        public LoginLockedException()
            : base("Too many attempts. Try again in a minute.") { }
    }
}
