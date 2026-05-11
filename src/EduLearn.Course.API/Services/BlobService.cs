using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace EduLearn.Course.API.Services
{
    public class BlobService : IBlobService
    {
        private const int DefaultSasMinutes = 30;
        private readonly BlobServiceClient? _blobServiceClient;
        private readonly string _containerName;
        private readonly string? _configurationError;
        private readonly int _sasExpiryMinutes;

        public BlobService(IConfiguration config)
        {
            var connectionString = config.GetSection("AzureStorage")["ConnectionString"];
            _containerName = config.GetSection("AzureStorage")["ContainerName"] ?? "course-thumbnails";
            _sasExpiryMinutes = ReadSasMinutes(config.GetSection("AzureStorage")["SasExpiryMinutes"]);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _configurationError = "AzureStorage:ConnectionString is missing from configuration.";
                return;
            }

            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
            catch (FormatException)
            {
                _configurationError = "AzureStorage:ConnectionString is invalid. Use a valid connection string or UseDevelopmentStorage=true.";
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (_blobServiceClient == null)
            {
                throw new InvalidOperationException(_configurationError ?? "Azure blob storage is not configured.");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

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
