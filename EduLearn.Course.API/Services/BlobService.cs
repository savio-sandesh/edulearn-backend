using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EduLearn.Course.API.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient? _blobServiceClient;
        private readonly string _containerName;
        private readonly string? _configurationError;

        public BlobService(IConfiguration config)
        {
            var connectionString = config.GetSection("AzureStorage")["ConnectionString"];
            _containerName = config.GetSection("AzureStorage")["ContainerName"] ?? "course-thumbnails";

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
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            return blobClient.Uri.ToString();
        }
    }
}
