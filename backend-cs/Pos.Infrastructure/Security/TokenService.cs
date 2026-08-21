using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PosCs.Infrastructure.Security
{
    /// <summary>HMAC-SHA256 bearer tokens: base64url(payload).base64url(signature), 12h expiry.</summary>
    public class TokenService : PosCs.Application.Ports.ITokenService
    {
        public string Issue(string secret, string userId)
        {
            var payload = $"{{\"sub\":\"{userId}\",\"exp\":{(long)(DateTime.UtcNow.AddHours(12) - new DateTime(1970, 1, 1)).TotalSeconds}}}";
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var sig = HmacSha256(secret, payloadB64);
            return payloadB64 + "." + sig;
        }

        public string Validate(string secret, string token)
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
                var json = JObject.Parse(payload);
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

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
