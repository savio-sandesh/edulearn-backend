using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace EduLearn.Auth.API.Services
{
    public class BlobService : IBlobService
    {
        private const int DefaultSasMinutes = 30;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly int _sasExpiryMinutes;

        public BlobService(IConfiguration config)
        {
            // appsettings.json se connection string uthana
            var connectionString = config.GetSection("AzureStorage")["ConnectionString"];
            _containerName = config.GetSection("AzureStorage")["ContainerName"] ?? "avatars";
            _sasExpiryMinutes = ReadSasMinutes(config.GetSection("AzureStorage")["SasExpiryMinutes"]);
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Azure Storage Connection String is missing!");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            // File ka unique naam banana (Guid use karke)
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };

            // Azure par upload karna
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });

            // Final URL return karna
            return GenerateReadSasUrl(blobClient, _containerName, uniqueFileName);
        }

        private string GenerateReadSasUrl(BlobClient blobClient, string containerName, string blobName)
        {
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Azure Storage connection string must include an account key to generate SAS URLs.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_sasExpiryMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var signedUri = blobClient.GenerateSasUri(sasBuilder);
            return signedUri.ToString();
        }

        private static int ReadSasMinutes(string? value)
        {
            if (int.TryParse(value, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            return DefaultSasMinutes;
        }
    }
}