using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Owin.Hosting;
using PosCs.Database.Migrations;
using PosCs.Helpers;

namespace PosCs
{
    public class Startup
    {
        private static byte[] _fallbackBytes;
        private static bool _fallbackAvailable;

        public void Configuration(IAppBuilder app)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                    MigrationRunner.ApplyPending(conn);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Migration failed: {ex.Message}");
            }

            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.None
            };
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            app.UseCors(CorsOptions.AllowAll);

            var wwwroot = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

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

                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength = _fallbackBytes.LongLength;
                await ctx.Response.Body.WriteAsync(_fallbackBytes, 0, _fallbackBytes.Length);
            });

            app.UseWebApi(config);

            Console.WriteLine("[API] Server started on port 3001");
        }

        public static void Start()
        {
            var url = $"http://localhost:{Environment.GetEnvironmentVariable("API_PORT") ?? "3001"}";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine($"[API] Listening on {url}");
                Console.WriteLine("[API] Press Ctrl+C to stop");
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }
    }
}
