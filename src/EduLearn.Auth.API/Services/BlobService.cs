using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EduLearn.Auth.API.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobService(IConfiguration config)
        {
            // appsettings.json se connection string uthana
            var connectionString = config.GetSection("AzureStorage")["ConnectionString"];
            _containerName = config.GetSection("AzureStorage")["ContainerName"] ?? "avatars";
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Azure Storage Connection String is missing!");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            
            // File ka unique naam banana (Guid use karke)
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };

            // Azure par upload karna
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });

            // Final URL return karna
            return blobClient.Uri.ToString();
        }
    }
}