using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WeatherImageFunctions
{
    public class GetResultsFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public GetResultsFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<GetResultsFunction>();
            _blobServiceClient = blobServiceClient;
        }

        [Function("GetResultsFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "images/results/{jobId}")] HttpRequestData req,
            string jobId)
        {
            _logger.LogInformation($"Fetching results for jobId: {jobId}");

            var containerClient = _blobServiceClient.GetBlobContainerClient("images");
            var blobs = containerClient.GetBlobsAsync();

            var results = new List<object>();

            await foreach (var blob in blobs)
            {
                if (blob.Name.StartsWith(jobId))
                {
                    var blobClient = containerClient.GetBlobClient(blob.Name);

                    // Nieuwe helper met GenerateSasUri
                    string sasUrl = BlobHelper.GenerateSasUrl(blobClient);

                    results.Add(new
                    {
                        blobName = blob.Name,
                        url = sasUrl,
                        metadata = blob.Metadata
                    });
                }
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);

            return response;
        }
    }
}
