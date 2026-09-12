using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Dapper;
using Newtonsoft.Json.Linq;

namespace PosCs.Infrastructure.Persistence
{
    public sealed class CloudinaryBackupUploader
    {
        private readonly string _cloudName;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _tenantId;

        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_cloudName)
                    && !string.IsNullOrWhiteSpace(_apiKey)
                    && !string.IsNullOrWhiteSpace(_apiSecret)
                    && !string.IsNullOrWhiteSpace(_tenantId);
            }
        }

        public CloudinaryBackupUploader()
        {
            _cloudName = Environment.GetEnvironmentVariable("POS_CLOUDINARY_CLOUD_NAME");
            _apiKey = Environment.GetEnvironmentVariable("POS_CLOUDINARY_API_KEY");
            _apiSecret = Environment.GetEnvironmentVariable("POS_CLOUDINARY_API_SECRET");
            _tenantId = Environment.GetEnvironmentVariable("POS_BACKUP_TENANT_ID") ?? "tenant-default";
        }

        public void UploadLatest(string filePath)
        {
            if (!IsConfigured) return;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Backup file was not found", filePath);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var tenantName = GetTenantName();
            var publicId = SanitizeSegment(tenantName) + "_latest.db";
            var endpoint = "https://api.cloudinary.com/v1_1/" + Uri.EscapeDataString(_cloudName) + "/raw/upload";
            const int chunkSize = 20 * 1024 * 1024;
            var uploadId = Guid.NewGuid().ToString("N");
            var totalLength = new FileInfo(filePath).Length;
            var usesChunkedUpload = totalLength > 100L * 1024L * 1024L;
            var metadataContext = BuildMetadataContext(tenantName, _tenantId, totalLength, DateTime.Now);
            long offset = 0;

            using (var client = new HttpClient())
            using (var stream = File.OpenRead(filePath))
            {
                client.Timeout = TimeSpan.FromMinutes(15);
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":" + _apiSecret));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                var buffer = new byte[chunkSize];
                while (offset < totalLength)
                {
                    var bytesToRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesToRead <= 0) break;
                    var end = offset + bytesToRead - 1;

                    using (var form = new MultipartFormDataContent())
                    using (var chunkStream = new MemoryStream(buffer, 0, bytesToRead, false))
                    using (var content = new StreamContent(chunkStream))
                    using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                    {
                        form.Add(new StringContent(publicId), "public_id");
                        form.Add(new StringContent("POS"), "asset_folder");
                        form.Add(new StringContent("true"), "use_asset_folder_as_public_id_prefix");
                        form.Add(new StringContent("true"), "overwrite");
                        form.Add(new StringContent("authenticated"), "type");
                        form.Add(new StringContent(metadataContext), "context");
                        form.Add(new StringContent("pos-backup,automatic-backup"), "tags");
                        form.Add(content, "file", publicId);
                        request.Content = form;
                        if (usesChunkedUpload)
                        {
                            request.Headers.TryAddWithoutValidation("X-Unique-Upload-Id", uploadId);
                            request.Headers.TryAddWithoutValidation("Content-Range", "bytes " + offset + "-" + end + "/" + totalLength);
                        }

                        using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                        {
                            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            if (!response.IsSuccessStatusCode)
                                throw new InvalidOperationException("Cloudinary upload failed: " + (string.IsNullOrWhiteSpace(body) ? response.StatusCode.ToString() : body));

                            if (end == totalLength - 1)
                            {
                                var json = JObject.Parse(body);
                                if (!string.Equals((string)json["resource_type"], "raw", StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("Cloudinary returned an unexpected resource type");

                                var returnedPublicId = (string)json["public_id"];
                                var returnedAssetFolder = (string)json["asset_folder"];
                                var legacyPublicId = "POS/" + publicId;
                                if (!string.Equals(returnedPublicId, publicId, StringComparison.Ordinal)
                                    && !string.Equals(returnedPublicId, legacyPublicId, StringComparison.Ordinal))
                                {
                                    var originalPublicId = returnedPublicId;
                                    returnedPublicId = RenameAsset(client, endpoint, returnedPublicId, publicId);
                                    Console.WriteLine("[BACKUP] Cloudinary asset normalized: " + originalPublicId + " -> " + returnedPublicId);
                                }

                                ApplyAssetMetadata(client, endpoint, returnedPublicId, metadataContext);
                                returnedAssetFolder = "POS";
                                if (!string.Equals(returnedPublicId, publicId, StringComparison.Ordinal)
                                    && !string.Equals(returnedPublicId, legacyPublicId, StringComparison.Ordinal))
                                    throw new InvalidOperationException("Cloudinary returned an unexpected public ID after normalization: " + returnedPublicId);

                                Console.WriteLine("[BACKUP] Cloudinary target verified: " + returnedAssetFolder + "/" + returnedPublicId);
                            }
                        }
                    }

                    offset = end + 1;
                }
            }
        }

        private static string RenameAsset(HttpClient client, string uploadEndpoint, string fromPublicId, string toPublicId)
        {
            var renameEndpoint = uploadEndpoint.Substring(0, uploadEndpoint.LastIndexOf("/", StringComparison.Ordinal)) + "/rename";
            var body = SendForm(client, renameEndpoint,
                new KeyValuePair<string, string>("from_public_id", fromPublicId),
                new KeyValuePair<string, string>("to_public_id", toPublicId),
                new KeyValuePair<string, string>("type", "upload"),
                new KeyValuePair<string, string>("overwrite", "true"));
            var json = JObject.Parse(body);
            var renamedPublicId = (string)json["public_id"];
            if (string.IsNullOrWhiteSpace(renamedPublicId))
                throw new InvalidOperationException("Cloudinary rename did not return a public ID");
            return renamedPublicId;
        }

        private static void ApplyAssetMetadata(HttpClient client, string uploadEndpoint, string publicId, string metadataContext)
        {
            var explicitEndpoint = uploadEndpoint.Substring(0, uploadEndpoint.LastIndexOf("/", StringComparison.Ordinal)) + "/explicit";
            SendForm(client, explicitEndpoint,
                new KeyValuePair<string, string>("public_id", publicId),
                new KeyValuePair<string, string>("type", "upload"),
                new KeyValuePair<string, string>("asset_folder", "POS"),
                new KeyValuePair<string, string>("context", metadataContext),
                new KeyValuePair<string, string>("tags", "pos-backup,automatic-backup"));
        }

        private static string SendForm(HttpClient client, string endpoint, params KeyValuePair<string, string>[] fields)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            using (var form = new FormUrlEncodedContent(fields))
            {
                request.Content = form;
                using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException("Cloudinary request failed: " + (string.IsNullOrWhiteSpace(body) ? response.StatusCode.ToString() : body));
                    return body;
                }
            }
        }

        private static string SanitizeSegment(string value)
        {
            var builder = new StringBuilder();
            var previousWasSeparator = false;
            foreach (var character in (value ?? string.Empty).Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasSeparator) builder.Append('_');
                    previousWasSeparator = true;
                }
                else if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                {
                    builder.Append(character);
                    previousWasSeparator = false;
                }
            }
            return builder.Length == 0 ? "tenant-default" : builder.ToString();
        }

        public static string BuildMetadataContext(string tenantName, string tenantId, long databaseSizeBytes, DateTime backupDate)
        {
            return "tenant_name=" + SanitizeMetadataValue(tenantName)
                + "|tenant_id=" + SanitizeMetadataValue(tenantId)
                + "|backup_date=" + backupDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                + "|app_version=v2"
                + "|database_size_bytes=" + databaseSizeBytes.ToString(CultureInfo.InvariantCulture);
        }

        private static string SanitizeMetadataValue(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("|", " ")
                .Replace("=", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private string GetTenantName()
        {
            try
            {
                using (var connection = DbConnectionFactory.CreateConnection())
                    return connection.ExecuteScalar<string>("SELECT name FROM Tenant WHERE id = @tenantId", new { tenantId = _tenantId }) ?? _tenantId;
            }
            catch
            {
                return _tenantId;
            }
        }
    }
}
