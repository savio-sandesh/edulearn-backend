using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace EduLearn.Progress.API.Services;

public class BlobStorageService : IBlobStorageService
{
    private const int DefaultSasMinutes = 30;
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly int _sasExpiryMinutes;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is missing from configuration.");
        _containerName = configuration["AzureStorage:ContainerName"] ?? "certificates";
        _sasExpiryMinutes = ReadSasMinutes(configuration["AzureStorage:SasExpiryMinutes"]);
    }

    public async Task<string> UploadCertificateAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(fileName);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, options);

        return GenerateReadSasUrl(blobClient, _containerName, fileName);
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
