using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PluginAutoCad.Services
{
    public class GeoService
    {
        private readonly HttpClient _client;
        private readonly string _token;

        public GeoService(string token)
        {
            _token = token;
            _client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        public async Task<string> GetWmsCapabilities(string baseUrl)
        {
            return await _client.GetStringAsync($"{baseUrl}?service=WMS&request=GetCapabilities");
        }

        public async Task<string> GetWfsCapabilities(string baseUrl)
        {
            return await _client.GetStringAsync($"{baseUrl}?service=WFS&request=GetCapabilities");
        }

        // Lấy ảnh preview từ WMS
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
                string fullLayer = layerName.Contains(":")? layerName: $"HoSoGis:{layerName}";

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

                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // tạo thư mục cache riêng cho plugin
                string tempDir = Path.Combine(Path.GetTempPath(), "AutoCAD_GIS");
                Directory.CreateDirectory(tempDir);

                string safeLayerName = layerName.Replace(":", "_");

                string tempFile = Path.Combine(
                    tempDir,
                    $"{safeLayerName}.png"
                );

                using (var fs = new FileStream(
                    tempFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await fs.WriteAsync(imageBytes, 0, imageBytes.Length);
                }

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
                $"&outputFormat=application/json";

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}