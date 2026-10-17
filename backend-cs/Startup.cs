using System;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Owin;
using System.Web.Cors;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Owin.Hosting;
using PosCs.Api;
using PosCs.Infrastructure.Config;
using PosCs.Infrastructure.Persistence;
using PosCs.Infrastructure.Data;
using PosCs.Middleware;

namespace PosCs
{
    public class Startup
    {
        private static byte[] _fallbackBytes;
        private static bool _fallbackAvailable;
        private static byte[] _inventorySessionFallbackBytes;
        private static bool _inventorySessionFallbackAvailable;

        public void Configuration(IAppBuilder app)
        {
            PosConfig.SyncToEnvironment();
            ConsoleLog.Install();

            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                    MigrationRunner.ApplyPending(conn);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Migration failed: {ex}");
                try
                {
                    var dataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    System.IO.Directory.CreateDirectory(dataDir);
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(dataDir, "startup-error.txt"),
                        DateTime.UtcNow.ToString("o") + Environment.NewLine + ex);
                }
                catch
                {
                }
                throw new InvalidOperationException("Database migration failed.", ex);
            }

            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new LegacyJsonContractResolver(),
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.None
            };
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // CORS is only enabled when POS_ENABLE_CORS is set. The production
            // self-hosted build serves the SPA same-origin, so cross-origin
            // requests should be blocked by default.
            var enableCors = !string.IsNullOrEmpty(PosConfig.Get("POS_ENABLE_CORS"));
            if (enableCors)
            {
                app.UseCors(new CorsOptions
                {
                    PolicyProvider = new CorsPolicyProvider
                    {
                        PolicyResolver = req =>
                        {
                            var policy = new CorsPolicy
                            {
                                AllowAnyMethod = true,
                                AllowAnyHeader = true,
                                SupportsCredentials = true
                            };
                            var origin = req.Headers.Get("Origin");
                            if (!string.IsNullOrEmpty(origin) &&
                                (origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
                                 origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)))
                            {
                                policy.Origins.Add(origin);
                            }
                            return Task.FromResult(policy);
                        }
                    }
                });
            }

            // Authenticate bearer tokens before any controller runs.
            app.Use<ApiAuthMiddleware>();

            var sensitivePrefixes = new[] { "/bin/", "/data/", "/migrations/", "/license.secret" };
            app.Use(async (ctx, next) =>
            {
                var p = ctx.Request.Path.Value ?? "";
                foreach (var prefix in sensitivePrefixes)
                {
                    if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = 403;
                        return;
                    }
                }
                await next();
            });

            var wwwroot = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            if (!System.IO.Directory.Exists(wwwroot))
                wwwroot = System.AppDomain.CurrentDomain.BaseDirectory;

            if (System.IO.Directory.Exists(wwwroot))
            {
                var mimeTypes = new FileExtensionContentTypeProvider();
                mimeTypes.Mappings[".js"] = "application/javascript";
                mimeTypes.Mappings[".css"] = "text/css";
                mimeTypes.Mappings[".woff2"] = "font/woff2";
                mimeTypes.Mappings[".svg"] = "image/svg+xml";
                mimeTypes.Mappings[".json"] = "application/json";
                mimeTypes.Mappings[".txt"] = "text/plain; charset=utf-8";

                var fileServerOptions = new FileServerOptions
                {
                    RequestPath = Microsoft.Owin.PathString.Empty,
                    FileSystem = new PhysicalFileSystem(wwwroot),
                    EnableDefaultFiles = true,
                    EnableDirectoryBrowsing = false,
                    StaticFileOptions = { ContentTypeProvider = mimeTypes }
                };
                fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
                fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html");
                fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("ar/index.html");
                app.UseFileServer(fileServerOptions);

                var indexHtmlPath = System.IO.Path.Combine(wwwroot, "ar", "index.html");
                if (System.IO.File.Exists(indexHtmlPath))
                {
                    _fallbackBytes = System.IO.File.ReadAllBytes(indexHtmlPath);
                    _fallbackAvailable = true;
                }

                var inventorySessionPath = System.IO.Path.Combine(wwwroot, "ar", "inventory-adjustments", "__session__", "index.html");
                if (System.IO.File.Exists(inventorySessionPath))
                {
                    _inventorySessionFallbackBytes = System.IO.File.ReadAllBytes(inventorySessionPath);
                    _inventorySessionFallbackAvailable = true;
                }
            }

            // SPA fallback BEFORE Web API — serves ar/index.html for non-API routes
            app.Use(async (ctx, next) =>
            {
                var path = ctx.Request.Path.Value ?? "";
                var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                         || path.Equals("/api", StringComparison.OrdinalIgnoreCase)
                         || path.Equals("/health", StringComparison.OrdinalIgnoreCase);

                if (isApi || !_fallbackAvailable)
                {
                    await next();
                    return;
                }

                var isInventorySession = path.StartsWith("/ar/inventory-adjustments/", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/ar/inventory-adjustments/new", StringComparison.OrdinalIgnoreCase);
                var fallback = isInventorySession && _inventorySessionFallbackAvailable
                    ? _inventorySessionFallbackBytes
                    : _fallbackBytes;
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength = fallback.LongLength;
                await ctx.Response.Body.WriteAsync(fallback, 0, fallback.Length);
            });

            app.UseWebApi(config);

            if (PosConfig.GetBool("POS_BACKUP_WORKER", false))
                EnsureBackupWorker();

            Console.WriteLine("[API] Server started on port 3001");
        }

        private static readonly object BackupWorkerLock = new object();
        private static CloudBackupWorker _backupWorker;

        private static void EnsureBackupWorker()
        {
            lock (BackupWorkerLock)
            {
                if (_backupWorker != null) return;
                _backupWorker = new CloudBackupWorker(CompositionRoot.BackupService, new CloudinaryBackupUploader());
            }
        }

        public static void Start()
        {
            var url = $"http://localhost:{Environment.GetEnvironmentVariable("API_PORT") ?? "3001"}";
            using (WebApp.Start<Startup>(url))
            {
                EnsureBackupWorker();
                Console.WriteLine($"[API] Listening on {url}");
                Console.WriteLine("[API] Press Ctrl+C to stop");
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }
    }
}
