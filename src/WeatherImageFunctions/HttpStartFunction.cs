using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherImageFunctions.Models;

namespace WeatherImageFunctions
{
    public class HttpStartFunction
    {
        private readonly ILogger _logger;
        private readonly QueueClient _queueClient;

        public HttpStartFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpStartFunction>();

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _queueClient = new QueueClient(connectionString, "jobs-start");
            _queueClient.CreateIfNotExists();
        }

        [Function("HttpStartFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "images/start")] HttpRequestData req)
        {
            _logger.LogInformation("Start request received...");

            string jobId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid()}";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = string.IsNullOrWhiteSpace(requestBody)
                ? new StartRequest()
                : JsonSerializer.Deserialize<StartRequest>(requestBody);

            var message = new
            {
                jobId = jobId,
                query = input?.Query ?? "clouds"
            };

            string messageJson = JsonSerializer.Serialize(message);

            await _queueClient.SendMessageAsync(
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson)));

            var response = req.CreateResponse(System.Net.HttpStatusCode.Accepted);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { jobId = jobId }));

            return response;
        }
    }
}
