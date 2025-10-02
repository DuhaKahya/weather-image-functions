using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace WeatherImageFunctions
{
    public class GetResultsFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public GetResultsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetResultsFunction>();

            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _blobServiceClient = new BlobServiceClient(storageConnectionString);
        }

        [Function("GetResultsFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/results/{jobId}")] HttpRequestData req,
            string jobId)
        {
            _logger.LogInformation($"Fetching results for job: {jobId}");

            var containerClient = _blobServiceClient.GetBlobContainerClient("images");
            var results = new List<object>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (blobItem.Name.Contains(jobId))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);

                    results.Add(new
                    {
                        FileName = blobItem.Name,
                        Url = blobClient.Uri.ToString(),
                        blobItem.Properties.ContentType,
                        blobItem.Properties.ContentLength
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            if (results.Count == 0)
            {
                await response.WriteAsJsonAsync(new
                {
                    jobId,
                    status = "running",
                    message = "No results yet for this job"
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
