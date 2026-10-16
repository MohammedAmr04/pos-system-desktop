using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Sync
{
    public class BranchSwitchClient : IBranchSwitchClient
    {
        public BranchAssignmentResult RequestCentralSwitch(BranchAgentConfig config, string currentBranchId, BranchSwitchRequest request)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.CentralBaseUrl))
                throw new DomainValidationException("Central server is not configured for this terminal");
            if (string.IsNullOrWhiteSpace(config.CentralToken))
                throw new DomainValidationException("Central sync credentials are not configured for this terminal");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) })
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.CentralToken);
                if (!string.IsNullOrWhiteSpace(currentBranchId))
                    http.DefaultRequestHeaders.Add("X-Branch-Id", currentBranchId);
                if (!string.IsNullOrWhiteSpace(config.BranchKey))
                    http.DefaultRequestHeaders.Add("X-Branch-Key", config.BranchKey);

                var url = config.CentralBaseUrl.TrimEnd('/') + "/api/branch/switch";
                var json = JsonConvert.SerializeObject(request);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = http.PostAsync(url, content).GetAwaiter().GetResult())
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new DomainValidationException("Central rejected the branch switch: " + ExtractError(body, response.StatusCode));
                    if (string.IsNullOrWhiteSpace(body))
                        throw new DomainValidationException("Central returned no result for the branch switch");
                    return JsonConvert.DeserializeObject<BranchAssignmentResult>(body);
                }
            }
        }

        public BranchAssignmentResult GetCentralAssignment(BranchAgentConfig config, string requestId)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.CentralBaseUrl))
                throw new DomainValidationException("Central server is not configured for this terminal");
            if (string.IsNullOrWhiteSpace(config.CentralToken))
                throw new DomainValidationException("Central sync credentials are not configured for this terminal");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) })
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.CentralToken);
                if (!string.IsNullOrWhiteSpace(config.BranchId))
                    http.DefaultRequestHeaders.Add("X-Branch-Id", config.BranchId);
                if (!string.IsNullOrWhiteSpace(config.BranchKey))
                    http.DefaultRequestHeaders.Add("X-Branch-Key", config.BranchKey);

                var url = config.CentralBaseUrl.TrimEnd('/') + "/api/branch/switch/" + Uri.EscapeDataString(requestId);
                using (var response = http.GetAsync(url).GetAwaiter().GetResult())
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new DomainValidationException("Central could not resolve the branch switch: " + ExtractError(body, response.StatusCode));
                    if (string.IsNullOrWhiteSpace(body)) return null;
                    return JsonConvert.DeserializeObject<BranchAssignmentResult>(body);
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
