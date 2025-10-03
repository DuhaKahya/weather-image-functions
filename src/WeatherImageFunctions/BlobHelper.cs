using System;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace WeatherImageFunctions
{
    public static class BlobHelper
    {
        public static string GenerateSasUrl(BlobClient blobClient)
        {
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("BlobClient cannot generate SAS URI. Make sure you use a credential that supports SAS (e.g. AccountKey or Managed Identity).");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }
    }
}
