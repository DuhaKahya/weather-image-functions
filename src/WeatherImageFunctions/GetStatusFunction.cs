using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WeatherImageFunctions
{
    public class GetStatusFunction
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<GetStatusFunction> _logger;

        public GetStatusFunction(BlobServiceClient blobServiceClient, ILogger<GetStatusFunction> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("GetStatusFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/status/{jobId}")] HttpRequestData req,
            string jobId)
        {
            _logger.LogInformation($"Checking status for job: {jobId}");

            var response = req.CreateResponse();

            var containerClient = _blobServiceClient.GetBlobContainerClient("images");
            await containerClient.CreateIfNotExistsAsync();

            var blobs = containerClient.GetBlobsAsync(prefix: jobId + "/");
            var results = new List<string>();

            await foreach (BlobItem blob in blobs)
            {
                results.Add($"{containerClient.Uri}/{blob.Name}");
            }

            if (results.Count == 0)
            {
                await response.WriteAsJsonAsync(new
                {
                    jobId,
                    status = "running",
                    message = "Job is still running or no results yet"
                });
            }
            else
            {
                await response.WriteAsJsonAsync(new
                {
                    jobId,
                    status = "completed",
                    results
                });
            }

            return response;
        }
    }
}
