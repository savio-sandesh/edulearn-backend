using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace EduLearn.Content.API.Services;

public class BlobService : IBlobService
{
    private const int DefaultSasMinutes = 30;
    private readonly string? _connectionString;
    private readonly string _defaultContainerName;
    private readonly int _sasExpiryMinutes;

    public BlobService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"];
        _defaultContainerName = configuration["AzureStorage:ContainerName"] ?? "lesson-content";
        _sasExpiryMinutes = ReadSasMinutes(configuration["AzureStorage:SasExpiryMinutes"]);
    }

    public Task<string> GenerateReadSasUrlAsync(string contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl) || string.IsNullOrWhiteSpace(_connectionString))
        {
            return Task.FromResult(contentUrl);
        }

        // If the stored value is a full absolute URL, try to reuse/validate it or convert
        if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri))
        {
            if (HasSasToken(contentUri))
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

            // Fix for local Azurite emulator where the first path segment is the account name
            if (contentUri.IsLoopback && containerName == "devstoreaccount1")
            {
                var azuriteSegments = blobName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (azuriteSegments.Length < 2) return Task.FromResult(contentUrl);
                containerName = azuriteSegments[0];
                blobName = azuriteSegments[1];
            }

            var blobServiceClient = new BlobServiceClient(_connectionString);
            if (!string.Equals(contentUri.Host, blobServiceClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(contentUrl);
            }

            var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            var sasUrl = GenerateReadSasUrl(blobClient, containerName, blobName);
            return Task.FromResult(sasUrl);
        }

        // If the stored value is not an absolute URL, treat it as a blob name in the default container
        var fallbackBlobServiceClient = new BlobServiceClient(_connectionString);
        var fallbackBlobClient = fallbackBlobServiceClient.GetBlobContainerClient(_defaultContainerName).GetBlobClient(contentUrl);
        var fallbackSas = GenerateReadSasUrl(fallbackBlobClient, _defaultContainerName, contentUrl);
        return Task.FromResult(fallbackSas);
    }

    private static int ReadSasMinutes(string? value)
    {
        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return DefaultSasMinutes;
    }

    public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType, string? containerName = null)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is not configured.");
        }

        var resolvedContainerName = string.IsNullOrWhiteSpace(containerName) ? _defaultContainerName : containerName;
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(resolvedContainerName);
        
        await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(fileName);
        
        var options = new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, options);

        return GenerateReadSasUrl(blobClient, resolvedContainerName, fileName);
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
            // Start time should be set in the past to avoid clock skew issues
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-60),
            // ExpiresOn extended for local/testing to reduce SAS expiry issues
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var signedUri = blobClient.GenerateSasUri(sasBuilder);
        return signedUri.ToString();
    }

    private static bool HasSasToken(Uri uri)
    {
        var query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return query.Contains("sig=", StringComparison.OrdinalIgnoreCase)
            && query.Contains("sv=", StringComparison.OrdinalIgnoreCase);
    }
}
