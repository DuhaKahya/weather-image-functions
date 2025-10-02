using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageFunctions.Models;

namespace WeatherImageFunctions
{
    public class ProcessImageFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _unsplashAccessKey;

        private static readonly HttpClient _httpClient = new HttpClient();

        public ProcessImageFunction(ILoggerFactory loggerFactory, IConfiguration config, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<ProcessImageFunction>();
            _blobServiceClient = blobServiceClient;
            _unsplashAccessKey = config["UnsplashApiKey"];
        }

        [Function("ProcessImageFunction")]
        public async Task Run([QueueTrigger("jobs-images", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            _logger.LogInformation($"ProcessImageFunction triggered with message: {queueMessage}");

            try
            {
                var job = JsonSerializer.Deserialize<ImageJobMessage>(queueMessage);
                if (job == null)
                {
                    _logger.LogError("Invalid message format");
                    return;
                }

                if (string.IsNullOrEmpty(_unsplashAccessKey))
                {
                    _logger.LogError("Unsplash API key is missing in local.settings.json");
                    return;
                }

                // Stap 1: haal Unsplash URL op
                string requestUrl = $"https://api.unsplash.com/photos/random?query={job.Query}&client_id={_unsplashAccessKey}";
                _logger.LogInformation($"Fetching Unsplash API: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                string imageUrl = doc.RootElement.GetProperty("urls").GetProperty("regular").GetString();

                _logger.LogInformation($"Downloading image from Unsplash: {imageUrl}");

                // Stap 2: download image
                var imgBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                using var ms = new MemoryStream(imgBytes);
                using var image = Image.FromStream(ms);

                // Stap 3: tekst overlay
                using (var graphics = Graphics.FromImage(image))
                using (var font = new Font("Arial", 20, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    string overlay = $"{job.StationName} {job.Temperature}°C {job.Humidity}%";
                    graphics.DrawString(overlay, font, brush, new PointF(10, 10));
                }

                // Stap 4: save to blob
                var containerClient = _blobServiceClient.GetBlobContainerClient("images");
                await containerClient.CreateIfNotExistsAsync();

                string blobName = $"{job.JobId}-{job.StationId}.png";
                var blobClient = containerClient.GetBlobClient(blobName);

                using var outStream = new MemoryStream();
                image.Save(outStream, ImageFormat.Png);
                outStream.Position = 0;

                await blobClient.UploadAsync(outStream, overwrite: true);

                // Stap 5: metadata toevoegen
                var metadata = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "stationName", job.StationName },
                    { "temperature", job.Temperature.ToString() },
                    { "humidity", job.Humidity.ToString() }
                };
                await blobClient.SetMetadataAsync(metadata);

                _logger.LogInformation($"Image saved to blob: {blobClient.Uri}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while processing image job: {ex.Message}");
            }
        }
    }
}
