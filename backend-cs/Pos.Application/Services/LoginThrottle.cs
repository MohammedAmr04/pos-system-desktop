using System;
using System.Collections.Generic;
using PosCs.Application.Ports;

namespace PosCs.Application.Services
{
    /// <summary>
    /// In-memory login throttle: after MaxFailures consecutive failures for the same
    /// username, further attempts are locked out for LockoutWindow.
    /// </summary>
    public sealed class LoginThrottle
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, (int Failures, DateTime LockedUntil)> _entries =
            new Dictionary<string, (int, DateTime)>();
        private readonly IClock _clock;

        private const int MaxFailures = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromSeconds(60);

        public LoginThrottle(IClock clock)
        {
            _clock = clock;
        }

        public bool IsLockedOut(string username)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(username, out var entry) && entry.LockedUntil > _clock.UtcNow)
                    return true;
                return false;
            }
        }

        public void RecordFailure(string username)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(username, out var entry))
                {
                    entry.Failures++;
                    if (entry.Failures >= MaxFailures)
                        entry.LockedUntil = _clock.UtcNow.Add(LockoutWindow);
                    _entries[username] = entry;
                }
                else
                {
                    _entries[username] = (1, DateTime.MinValue);
                }
            }
        }

        public void ResetThrottle(string username)
        {
            lock (_lock)
                _entries.Remove(username);
        }
    }
}
