using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Sync
{
    /// <summary>Proves a branch can reach central and that central accepts its branch key before
    /// the terminal persists any enrollment. Failures throw so setup does not save bad config.</summary>
    public class CentralEnrollmentClient : IBranchEnrollmentClient
    {
        public BranchEnrollmentResult VerifyCentral(string centralBaseUrl, string branchId, string branchKey, string syncToken)
        {
            if (string.IsNullOrWhiteSpace(centralBaseUrl))
                throw new DomainValidationException("Central server URL is required");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var baseUrl = centralBaseUrl.TrimEnd('/');

            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) })
            {
                try
                {
                    using (var health = http.GetAsync(baseUrl + "/api/health").GetAwaiter().GetResult())
                    {
                        if (!health.IsSuccessStatusCode)
                            throw new DomainValidationException("Central server did not respond to the health check (HTTP " + (int)health.StatusCode + ")");
                    }
                }
                catch (DomainValidationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new DomainValidationException("Central server is unreachable: " + ex.Message);
                }

                if (!string.IsNullOrWhiteSpace(syncToken))
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", syncToken.Trim());
                if (!string.IsNullOrWhiteSpace(branchId))
                    http.DefaultRequestHeaders.Add("X-Branch-Id", branchId.Trim());
                if (!string.IsNullOrWhiteSpace(branchKey))
                    http.DefaultRequestHeaders.Add("X-Branch-Key", branchKey.Trim());

                using (var response = http.GetAsync(baseUrl + "/api/branch/enroll").GetAwaiter().GetResult())
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new DomainValidationException("Central rejected the branch enrollment: " + ExtractError(body, response.StatusCode));
                    if (string.IsNullOrWhiteSpace(body)) return new BranchEnrollmentResult { BranchId = branchId, Verified = false };
                    var result = JsonConvert.DeserializeObject<BranchEnrollmentResult>(body);
                    return result ?? new BranchEnrollmentResult { BranchId = branchId, Verified = false };
                }
            }
        }

        private static string ExtractError(string body, HttpStatusCode status)
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ErrorBody>(body);
                    if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Message)) return parsed.Message;
                    if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Error)) return parsed.Error;
                }
                catch { }
                return body;
            }
            return "HTTP " + (int)status;
        }

        private sealed class ErrorBody
        {
            public string Message { get; set; }
            public string Error { get; set; }
        }
    }
}
