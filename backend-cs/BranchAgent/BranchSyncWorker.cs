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

        private void PushPending()
        {
            var pending = Get<List<SyncOperation>>(_config.LocalBaseUrl + "/api/sync/pending?limit=50");
            if (pending == null || pending.Count == 0) return;

            Post(_config.CentralBaseUrl + "/api/sync/receive", new SyncPushRequest { Operations = pending });
            foreach (var operation in pending)
                Post(_config.LocalBaseUrl + "/api/sync/" + Uri.EscapeDataString(operation.Id) + "/ack", new { });

            Console.WriteLine("[AGENT] Pushed " + pending.Count + " operation(s)");
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
            Post(_config.LocalBaseUrl + "/api/sync/receive", new SyncPushRequest { Operations = operations });
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
    }
}
