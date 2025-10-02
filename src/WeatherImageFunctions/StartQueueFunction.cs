using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WeatherImageFunctions
{
    public static class StartQueueFunction
    {
        [Function("StartQueueFunction")]
        [QueueOutput("jobs-images", Connection = "AzureWebJobsStorage")]
        public static string[] Run(
            [QueueTrigger("jobs-start", Connection = "AzureWebJobsStorage")] string startMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger("StartQueueFunction");
            logger.LogInformation($"StartQueueFunction triggered: {startMessage}");

            var messages = new List<string>();

            try
            {
                var json = JsonDocument.Parse(startMessage).RootElement;

                // jobId en query uitlezen
                string jobId = json.TryGetProperty("jobId", out var jid)
                    ? jid.GetString() ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

                string query = json.TryGetProperty("query", out var q)
                    ? q.GetString() ?? "clouds"
                    : "clouds";

                // Dummy stations (later vervang je dit met Buienradar API)
                var stations = GetDummyStations();

                foreach (var station in stations.EnumerateArray())
                {
                    int stationId = station.GetProperty("stationid").GetInt32();
                    string stationName = station.GetProperty("stationname").GetString() ?? "unknown";
                    double temperature = station.GetProperty("temperature").GetDouble();
                    double humidity = station.TryGetProperty("humidity", out var h) ? h.GetDouble() : 0.0;

                    var imageJob = new
                    {
                        JobId = jobId,
                        Query = query,
                        StationId = stationId,
                        StationName = stationName,
                        Temperature = temperature,
                        Humidity = humidity
                    };

                    string jsonMessage = JsonSerializer.Serialize(imageJob);
                    messages.Add(jsonMessage);

                    logger.LogInformation($"Queueing image job: {jsonMessage}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in StartQueueFunction: {ex.Message}");
            }

            return messages.ToArray();
        }

        private static JsonElement GetDummyStations()
        {
            string dummyJson = @"[
                { ""stationid"": 1, ""stationname"": ""Schiphol"", ""temperature"": 15.5, ""humidity"": 80 },
                { ""stationid"": 2, ""stationname"": ""Rotterdam"", ""temperature"": 16.2 }
            ]";

            return JsonDocument.Parse(dummyJson).RootElement;
        }
    }
}
