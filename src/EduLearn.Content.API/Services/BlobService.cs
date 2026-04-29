using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace EduLearn.Content.API.Services;

public class BlobService : IBlobService
{
    private const int DefaultSasMinutes = 30;
    private readonly string? _connectionString;
    private readonly int _sasExpiryMinutes;

    public BlobService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"];
        _sasExpiryMinutes = ReadSasMinutes(configuration["AzureStorage:SasExpiryMinutes"]);
    }

    public Task<string> GenerateReadSasUrlAsync(string contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl) || string.IsNullOrWhiteSpace(_connectionString))
        {
            return Task.FromResult(contentUrl);
        }

        if (!Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri))
        {
            return Task.FromResult(contentUrl);
        }

        var path = contentUri.AbsolutePath.Trim('/');
        var segments = path.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return Task.FromResult(contentUrl);
        }

        var containerName = segments[0];
        var blobName = Uri.UnescapeDataString(segments[1]);

        var blobServiceClient = new BlobServiceClient(_connectionString);
        if (!string.Equals(contentUri.Host, blobServiceClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(contentUrl);
        }

        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        if (!blobClient.CanGenerateSasUri)
        {
            return Task.FromResult(contentUrl);
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
        return Task.FromResult(signedUri.ToString());
    }

    private static int ReadSasMinutes(string? value)
    {
        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return DefaultSasMinutes;
    }

    public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType, string containerName = "lesson-videos")
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        
        await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(fileName);
        
        var options = new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, options);

        return blobClient.Uri.ToString();
    }
}
