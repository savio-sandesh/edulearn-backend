using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

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
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            return uniqueFileName;
        }

        /// <summary>
        /// Generates a read-only SAS URL for a blob file.
        /// </summary>
        public async Task<string> GenerateReadSasUrlAsync(string fileName, string containerName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            if (_blobServiceClient == null)
            {
                throw new InvalidOperationException(_configurationError ?? "Azure blob storage is not configured.");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Azure Storage connection string must include an account key to generate SAS URLs.");
            }

            // 24 hour expiry for read-only access (use UTC now to avoid clock skew issues)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}
