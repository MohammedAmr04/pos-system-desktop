using System;

namespace PosCs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== POS C# Backend ===");
            Console.WriteLine($"[API] Data directory: {Helpers.DbConnectionFactory.DbPath}");

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("[API] Shutting down...");
                Environment.Exit(0);
            };

            Startup.Start();
        }
    }
}
