using System;
using System.IO;

namespace PosCs.BranchAgent
{
    internal sealed class AgentConfig
    {
        public string LocalBaseUrl { get; private set; }
        public string CentralBaseUrl { get; private set; }
        public string Token { get; private set; }
        public string BranchId { get; private set; }
        public string BranchKey { get; private set; }
        public string NodeRole { get; private set; }
        public string CursorPath { get; private set; }
        public int PollSeconds { get; private set; }

        /// <summary>Applies identity/credentials resolved from the local backend so a terminal
        /// branch switch takes effect without editing environment variables or restarting.</summary>
        public void Apply(string centralBaseUrl, string token, string branchId, string branchKey, string nodeRole)
        {
            if (!string.IsNullOrWhiteSpace(centralBaseUrl)) CentralBaseUrl = centralBaseUrl.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(token)) Token = token;
            if (!string.IsNullOrWhiteSpace(branchId)) BranchId = branchId.Trim();
            if (!string.IsNullOrWhiteSpace(branchKey)) BranchKey = branchKey.Trim();
            if (!string.IsNullOrWhiteSpace(nodeRole)) NodeRole = nodeRole.Trim();
        }

        /// <summary>Env vars are an optional override for backward compatibility; the authoritative
        /// identity now comes from the local backend via <see cref="Apply"/>. Never throws so the
        /// agent can start unconditionally and idle until the node is enrolled.</summary>
        public static AgentConfig FromEnvironment()
        {
            var central = Environment.GetEnvironmentVariable("POS_CENTRAL_URL");
            var token = Environment.GetEnvironmentVariable("POS_SYNC_TOKEN");

            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            return new AgentConfig
            {
                LocalBaseUrl = (Environment.GetEnvironmentVariable("POS_BRANCH_LOCAL_URL") ?? "http://localhost:3001").TrimEnd('/'),
                CentralBaseUrl = central == null ? string.Empty : central.TrimEnd('/'),
                Token = token ?? string.Empty,
                BranchId = (Environment.GetEnvironmentVariable("POS_BRANCH_ID") ?? string.Empty).Trim(),
                BranchKey = (Environment.GetEnvironmentVariable("POS_BRANCH_KEY") ?? string.Empty).Trim(),
                NodeRole = (Environment.GetEnvironmentVariable("POS_NODE_ROLE") ?? string.Empty).Trim(),
                CursorPath = Path.Combine(dataDir, "sync-cursor.txt"),
                PollSeconds = ParsePollSeconds(Environment.GetEnvironmentVariable("POS_SYNC_POLL_SECONDS"))
            };
        }

        private static int ParsePollSeconds(string value)
        {
            int result;
            return int.TryParse(value, out result) && result >= 2 && result <= 300 ? result : 5;
        }
    }
}
