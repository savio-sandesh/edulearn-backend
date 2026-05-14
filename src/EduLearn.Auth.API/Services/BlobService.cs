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

            // Return only the stored blob name. SAS URLs should be generated dynamically when requested.
            return uniqueFileName;
        }

        /// <summary>
        /// Generates a read-only SAS URL for a stored blob file.
        /// </summary>
        public Task<string> GenerateReadSasUrlAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return Task.FromResult(string.Empty);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Azure Storage connection string must include an account key to generate SAS URLs.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var uri = blobClient.GenerateSasUri(sasBuilder).ToString();
            return Task.FromResult(uri);
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