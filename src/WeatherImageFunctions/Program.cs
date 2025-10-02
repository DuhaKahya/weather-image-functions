using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Haal connection string uit settings
var storageConnectionString = builder.Configuration["AzureWebJobsStorage"];

// Maak BlobServiceClient en container aan
var blobServiceClient = new BlobServiceClient(storageConnectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("images");
containerClient.CreateIfNotExists();

// Registreer BlobServiceClient voor dependency injection
builder.Services.AddSingleton(blobServiceClient);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
