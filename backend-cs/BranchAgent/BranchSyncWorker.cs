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
            var changes = Get<List<SyncChange>>(_config.CentralBaseUrl + "/api/sync/changes?afterVersion=" + cursor + "&limit=100");
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

        private T Get<T>(string url)
        {
            var response = _http.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }

        private void Post(string url, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
            {
                var response = _http.PostAsync(url, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
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
