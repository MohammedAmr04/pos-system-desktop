using System;
using System.IO;

namespace PosCs.BranchAgent
{
    internal sealed class AgentConfig
    {
        public string LocalBaseUrl { get; private set; }
        public string CentralBaseUrl { get; private set; }
        public string Token { get; private set; }
        public string CursorPath { get; private set; }
        public int PollSeconds { get; private set; }

        public static AgentConfig FromEnvironment()
        {
            var central = Environment.GetEnvironmentVariable("POS_CENTRAL_URL");
            var token = Environment.GetEnvironmentVariable("POS_SYNC_TOKEN");
            if (string.IsNullOrWhiteSpace(central)) throw new InvalidOperationException("POS_CENTRAL_URL is required");
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("POS_SYNC_TOKEN is required");

            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            return new AgentConfig
            {
                LocalBaseUrl = (Environment.GetEnvironmentVariable("POS_BRANCH_LOCAL_URL") ?? "http://localhost:3001").TrimEnd('/'),
                CentralBaseUrl = central.TrimEnd('/'),
                Token = token,
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
