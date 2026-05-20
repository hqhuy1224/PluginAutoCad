using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoCad.Services
{
    public class GeoService
    {
        private readonly HttpClient _client;
        private readonly string _token;
        private readonly string _username;

        public GeoService(string token)
        {
            _token = token;
            _client = new HttpClient();

            if (!string.IsNullOrEmpty(token))
            {
                _username = GetUsernameFromToken(token);
            }
        }

        public async Task<string> GetWmsCapabilities(string baseUrl)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}?service=WMS&request=GetCapabilities"
            );

            request.Headers.Add("X-Auth-User", _username);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetWfsCapabilities(string baseUrl)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}?service=WFS&request=GetCapabilities"
            );

            request.Headers.Add("X-Auth-User", _username);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }



        private string GetUsernameFromToken(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new Exception("Invalid JWT");

            string payload = parts[1];

            payload = payload.Replace('-', '+').Replace('_', '/');

            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);

            var obj = JObject.Parse(json);

            return obj["preferred_username"]?.ToString()
                ?? obj["username"]?.ToString()
                ?? obj["sub"]?.ToString();
        }

        public async Task<string> GetWmsMapImageAsync(
            string baseUrl,
            string layerName,
            double minX, double minY,
            double maxX, double maxY,
            int width = 1200,
            int height = 800,
            string srs = "EPSG:4326")
        {
            try
            {
                string fullLayer = layerName.Contains(":")
                    ? layerName
                    : $"HoSoGis:{layerName}";

                string url =
                    $"{baseUrl}?service=WMS" +
                    $"&request=GetMap" +
                    $"&version=1.1.0" +
                    $"&layers={fullLayer}" +
                    $"&bbox={minX},{minY},{maxX},{maxY}" +
                    $"&width={width}" +
                    $"&height={height}" +
                    $"&srs={srs}" +
                    $"&format=image/png" +
                    $"&transparent=true";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Auth-User", _username);

                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                string tempDir = Path.Combine(Path.GetTempPath(), "AutoCAD_GIS");
                Directory.CreateDirectory(tempDir);

                string safeLayerName = layerName.Replace(":", "_");

                string tempFile = Path.Combine(
                    tempDir,
                    $"{safeLayerName}.png"
                );

                File.WriteAllBytes(tempFile, imageBytes);

                return tempFile;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy ảnh WMS: {ex.Message}");
            }
        }

        public async Task<string> GetWfsFeaturesAsync(string baseUrl, string layerName)
        {
            string fullLayer = layerName.Contains(":")
                ? layerName
                : $"HoSoGis:{layerName}";

            string url =
                $"{baseUrl}?" +
                $"service=WFS" +
                $"&version=2.0.0" +
                $"&request=GetFeature" +
                $"&typeName={fullLayer}" +
                $"&outputFormat=application/json"+
                $"&srsName=EPSG:3857"; 

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-User", _username);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}