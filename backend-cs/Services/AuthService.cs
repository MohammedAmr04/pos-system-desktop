using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PosCs.Models;
using PosCs.Repositories;

namespace PosCs.Services
{
    public class AccessBundle
    {
        public User User { get; set; }
        public string TenantId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> Features { get; set; } = new List<string>();
    }

    public static class AuthService
    {
        private static readonly object _throttleLock = new object();
        private static readonly Dictionary<string, (int Failures, DateTime LockedUntil)> _loginThrottle =
            new Dictionary<string, (int, DateTime)>();
        private const int MaxFailures = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromSeconds(60);

        private static readonly AuthRepository _repo = new AuthRepository();

        // ---- Password hashing (PBKDF2 / SHA-256) ----
        public static string HashPassword(string password)
        {
            const int iterations = 100000;
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(32);
                return $"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
            }
        }

        public static bool VerifyPassword(string passwordHash, string password)
        {
            if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
                return false;
            var parts = passwordHash.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2")
                return false;

            int iterations;
            if (!int.TryParse(parts[1], out iterations) || iterations <= 0)
                return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch
            {
                return false;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var actual = pbkdf2.GetBytes(expected.Length);
                return FixedTimeEquals(actual, expected);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ---- HMAC bearer tokens ----
        public static string IssueToken(string secret, string userId)
        {
            var payload = $"{{\"sub\":\"{userId}\",\"exp\":{(long)(DateTime.UtcNow.AddHours(12) - new DateTime(1970, 1, 1)).TotalSeconds}}}";
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var sig = HmacSha256(secret, payloadB64);
            return payloadB64 + "." + sig;
        }

        public static string ValidateToken(string secret, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            var parts = token.Split('.');
            if (parts.Length != 2)
                return null;

            var expectedSig = HmacSha256(secret, parts[0]);
            if (!FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSig), Encoding.UTF8.GetBytes(parts[1])))
                return null;

            try
            {
                var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
                var json = Newtonsoft.Json.Linq.JObject.Parse(payload);
                var exp = (long)json["exp"];
                var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (exp < now)
                    return null;
                return (string)json["sub"];
            }
            catch
            {
                return null;
            }
        }

        private static string HmacSha256(string secret, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var s = value.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        // ---- Login (with throttle) ----
        public static bool IsLockedOut(string username)
        {
            lock (_throttleLock)
            {
                if (_loginThrottle.TryGetValue(username, out var entry) && entry.LockedUntil > DateTime.UtcNow)
                    return true;
                return false;
            }
        }

        public static void RecordFailure(string username)
        {
            lock (_throttleLock)
            {
                if (_loginThrottle.TryGetValue(username, out var entry))
                {
                    entry.Failures++;
                    if (entry.Failures >= MaxFailures)
                        entry.LockedUntil = DateTime.UtcNow.Add(LockoutWindow);
                    _loginThrottle[username] = entry;
                }
                else
                {
                    _loginThrottle[username] = (1, DateTime.MinValue);
                }
            }
        }

        public static void ResetThrottle(string username)
        {
            lock (_throttleLock)
                _loginThrottle.Remove(username);
        }

        public static (AccessBundle Bundle, string Token) Login(SqliteConnection conn, string username, string password)
        {
            var user = _repo.GetUserByUsername(conn, username);
            if (user == null || !user.IsActive)
                return (null, null);
            if (!VerifyPassword(user.PasswordHash, password))
                return (null, null);

            var bundle = BuildBundle(conn, user);
            var secret = _repo.EnsureTokenSecret(conn);
            return (bundle, IssueToken(secret, user.Id));
        }

        public static AccessBundle BuildBundle(SqliteConnection conn, User user)
        {
            var bundle = new AccessBundle
            {
                User = user,
                TenantId = _repo.GetTenantIdForUser(conn, user.Id),
                Roles = _repo.GetRoleIdsForUser(conn, user.Id)
                    .Select(id => new RolesRepository().GetById(conn, id)?.Name ?? id).ToList(),
                Permissions = _repo.GetPermissionKeysForUser(conn, user.Id).ToList()
            };

            if (!string.IsNullOrEmpty(bundle.TenantId))
                bundle.Features = _repo.GetEnabledFeatureKeys(conn, bundle.TenantId).ToList();

            return bundle;
        }

        public static AccessBundle GetBundleForToken(SqliteConnection conn, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            var secret = _repo.EnsureTokenSecret(conn);
            var userId = ValidateToken(secret, token);
            if (userId == null)
                return null;
            var user = _repo.GetUserById(conn, userId);
            if (user == null || !user.IsActive)
                return null;
            return BuildBundle(conn, user);
        }
    }
}
