using System;

namespace PosCs.BranchAgent
{
    internal static class Program
    {
        private static void Main()
        {
            var config = AgentConfig.FromEnvironment();
            var worker = new BranchSyncWorker(config);
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                worker.Stop();
            };

            Console.WriteLine("[AGENT] Branch sync agent started");
            worker.Run();
        }
    }
}
