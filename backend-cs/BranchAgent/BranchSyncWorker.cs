using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using PosCs.Application.Models;

namespace PosCs.BranchAgent
{
    internal sealed class BranchSyncWorker
    {
        private readonly AgentConfig _config;
        private readonly HttpClient _http;
        private volatile bool _running = true;

        public BranchSyncWorker(AgentConfig config)
        {
            _config = config;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
            if (!string.IsNullOrWhiteSpace(_config.BranchId))
                _http.DefaultRequestHeaders.Add("X-Branch-Id", _config.BranchId);
            if (!string.IsNullOrWhiteSpace(_config.BranchKey))
                _http.DefaultRequestHeaders.Add("X-Branch-Key", _config.BranchKey);
        }

        public void Stop()
        {
            _running = false;
        }

        public void Run()
        {
            while (_running)
            {
                try
                {
                    RefreshIdentity();
                    PushPending();
                    PullChanges();
                    RetryPrintQueue();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[AGENT ERR] " + ex.Message);
                }

                if (_running) Thread.Sleep(TimeSpan.FromSeconds(_config.PollSeconds));
            }
            _http.Dispose();
        }

        /// <summary>Reloads central URL + branch credentials from the local backend so a terminal
        /// branch switch propagates to the running agent without a restart.</summary>
        private void RefreshIdentity()
        {
            try
            {
                var cfg = Get<BranchAgentConfigDto>(_config.LocalBaseUrl + "/api/branch/agent-config");
                if (cfg == null) return;

                var changed = !string.Equals(_config.BranchId, cfg.BranchId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(_config.BranchKey, cfg.BranchKey ?? string.Empty, StringComparison.Ordinal)
                    || !string.Equals(_config.Token, cfg.CentralToken ?? string.Empty, StringComparison.Ordinal)
                    || !string.Equals(_config.CentralBaseUrl, (cfg.CentralBaseUrl ?? string.Empty).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                if (!changed) return;

                _config.Apply(cfg.CentralBaseUrl, cfg.CentralToken, cfg.BranchId, cfg.BranchKey);

                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
                _http.DefaultRequestHeaders.Remove("X-Branch-Id");
                _http.DefaultRequestHeaders.Remove("X-Branch-Key");
                if (!string.IsNullOrWhiteSpace(_config.BranchId))
                    _http.DefaultRequestHeaders.Add("X-Branch-Id", _config.BranchId);
                if (!string.IsNullOrWhiteSpace(_config.BranchKey))
                    _http.DefaultRequestHeaders.Add("X-Branch-Key", _config.BranchKey);

                Console.WriteLine("[AGENT] Branch identity refreshed -> " +
                    (string.IsNullOrWhiteSpace(_config.BranchId) ? "unconfigured-branch" : _config.BranchId));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[AGENT WARN] Could not refresh branch identity: " + ex.Message);
            }
        }

        private void PushPending()
        {
            var pending = Get<List<SyncOperation>>(_config.LocalBaseUrl + "/api/sync/pending?limit=50");
            if (pending == null || pending.Count == 0) return;

            var result = Post<SyncReceiveResult>(_config.CentralBaseUrl + "/api/sync/receive", new SyncPushRequest { Operations = pending });
            if (result == null)
            {
                Console.Error.WriteLine("[AGENT ERR] Central receive returned no result; not acknowledging " + pending.Count + " operation(s)");
                return;
            }
            var failures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (result.Failed != null)
            {
                foreach (var failure in result.Failed)
                {
                    if (failure == null || string.IsNullOrWhiteSpace(failure.Id)) continue;
                    failures[failure.Id] = failure.Error;
                }
            }

            foreach (var operation in pending)
            {
                string error;
                if (failures.TryGetValue(operation.Id, out error))
                {
                    Post(_config.LocalBaseUrl + "/api/sync/" + Uri.EscapeDataString(operation.Id) + "/attempt",
                        new { error = string.IsNullOrWhiteSpace(error) ? "Central apply failed" : error });
                }
                else
                {
                    Post(_config.LocalBaseUrl + "/api/sync/" + Uri.EscapeDataString(operation.Id) + "/ack", new { });
                }
            }

            Console.WriteLine("[AGENT] Pushed " + (pending.Count - failures.Count) + " operation(s), " + failures.Count + " failed");
        }

        private void PullChanges()
        {
            var cursor = ReadCursor();
            var branchQuery = string.IsNullOrWhiteSpace(_config.BranchId)
                ? string.Empty
                : "&branchId=" + Uri.EscapeDataString(_config.BranchId);
            var changes = Get<List<SyncChange>>(_config.CentralBaseUrl + "/api/sync/changes?afterVersion=" + cursor + "&limit=100" + branchQuery);
            if (changes == null || changes.Count == 0) return;

            var operations = new List<SyncOperation>();
            foreach (var change in changes)
            {
                operations.Add(new SyncOperation
                {
                    Id = "central-change-" + change.Version,
                    BranchId = string.IsNullOrEmpty(change.BranchId) ? "global" : change.BranchId,
                    TerminalId = "central",
                    OperationType = change.OperationType,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Payload = change.Payload,
                    OccurredAt = change.CreatedAt
                });
            }
            var applied = Post<SyncReceiveResult>(_config.LocalBaseUrl + "/api/sync/receive", new SyncPushRequest { Operations = operations });
            if (applied == null || !applied.Success)
            {
                var failedCount = applied == null ? changes.Count : applied.Failed.Count;
                Console.Error.WriteLine("[AGENT ERR] " + failedCount + " change(s) failed to apply locally; cursor not advanced");
                return;
            }
            WriteCursor(changes[changes.Count - 1].Version);
            Console.WriteLine("[AGENT] Pulled " + changes.Count + " change(s)");
        }

        private void RetryPrintQueue()
        {
            var jobs = Get<List<PrintJob>>(_config.LocalBaseUrl + "/api/printing/queue/pending?limit=10");
            if (jobs == null || jobs.Count == 0) return;

            foreach (var job in jobs)
            {
                try
                {
                    var payload = JsonConvert.DeserializeObject<object>(job.Payload);
                    Post(_config.LocalBaseUrl + "/api/printing/print", payload, true);
                    Post(_config.LocalBaseUrl + "/api/printing/queue/" + Uri.EscapeDataString(job.Id) + "/printed", new { });
                    Console.WriteLine("[AGENT] Printed queued job " + job.Id);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Post(_config.LocalBaseUrl + "/api/printing/queue/" + Uri.EscapeDataString(job.Id) + "/failed", new { error = ex.Message });
                    }
                    catch (Exception markError)
                    {
                        Console.Error.WriteLine("[AGENT ERR] Could not mark print job: " + markError.Message);
                    }
                }
            }
        }

        private T Get<T>(string url)
        {
            var response = _http.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }

        private void Post(string url, object payload, bool queueRetry = false)
        {
            var json = JsonConvert.SerializeObject(payload);
            using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = content;
                    if (queueRetry) request.Headers.Add("X-Print-Queue-Retry", "1");
                    var response = _http.SendAsync(request).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        private T Post<T>(string url, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
            {
                var response = _http.PostAsync(url, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return string.IsNullOrWhiteSpace(body) ? default(T) : JsonConvert.DeserializeObject<T>(body);
            }
        }

        private long ReadCursor()
        {
            long cursor;
            return File.Exists(_config.CursorPath) && long.TryParse(File.ReadAllText(_config.CursorPath), out cursor) ? cursor : 0;
        }

        private void WriteCursor(long cursor)
        {
            var temp = _config.CursorPath + ".tmp";
            File.WriteAllText(temp, cursor.ToString());
            if (File.Exists(_config.CursorPath)) File.Replace(temp, _config.CursorPath, null);
            else File.Move(temp, _config.CursorPath);
        }

        private sealed class BranchAgentConfigDto
        {
            public string CentralBaseUrl { get; set; }
            public string CentralToken { get; set; }
            public string BranchId { get; set; }
            public string BranchKey { get; set; }
            public string NodeRole { get; set; }
        }
    }
}
