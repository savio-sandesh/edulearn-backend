using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace EduLearn.Shared.Services;

public class SharedBlobService : ISharedBlobService
{
    private readonly IConfiguration _configuration;
    private readonly string? _connectionString;

    public SharedBlobService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration["AzureStorage:ConnectionString"];
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? containerName = null)
    {
        return await UploadInternalAsync(fileStream, fileName, contentType, containerName);
    }

    public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType, string? containerName = null)
    {
        return await UploadInternalAsync(fileStream, fileName, contentType, containerName);
    }

    private async Task<string> UploadInternalAsync(Stream fileStream, string fileName, string contentType, string? containerName)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is missing from configuration.");
        }

        var resolvedContainer = string.IsNullOrWhiteSpace(containerName) ? _configuration["AzureStorage:ContainerName"] ?? "avatars" : containerName;
        var client = new BlobServiceClient(_connectionString);
        var containerClient = client.GetBlobContainerClient(resolvedContainer);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await blobClient.UploadAsync(fileStream, options);

        // Persist only the blob name; callers can request a SAS URL dynamically.
        return uniqueFileName;
    }

    public Task<string> GenerateReadSasUrlAsync(string contentIdentifier, string? containerName = null)
    {
        if (string.IsNullOrWhiteSpace(contentIdentifier) || string.IsNullOrWhiteSpace(_connectionString))
        {
            return Task.FromResult(contentIdentifier ?? string.Empty);
        }

        // If contentIdentifier is an absolute URL, attempt to extract container/blob and generate SAS
        if (Uri.TryCreate(contentIdentifier, UriKind.Absolute, out var uri))
        {
            // If URL already contains SAS token, return as-is
            if (!string.IsNullOrWhiteSpace(uri.Query) && uri.Query.Contains("sig=", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(contentIdentifier);
            }

            // Extract blob path
            var path = uri.AbsolutePath.Trim('/');
            var segments = path.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return Task.FromResult(contentIdentifier);
            }

            var possibleContainer = segments[0];
            var possibleBlob = Uri.UnescapeDataString(segments[1]);

            // Handle Azurite local dev path where the account name appears first
            if (uri.IsLoopback && possibleContainer == "devstoreaccount1")
            {
                var azSegments = possibleBlob.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (azSegments.Length >= 2)
                {
                    possibleContainer = azSegments[0];
                    possibleBlob = azSegments[1];
                }
            }

            try
            {
                var client = new BlobServiceClient(_connectionString);
                if (!string.Equals(uri.Host, client.Uri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(contentIdentifier);
                }

                var blobClient = client.GetBlobContainerClient(possibleContainer).GetBlobClient(possibleBlob);
                return Task.FromResult(GenerateSas(blobClient, possibleContainer, possibleBlob));
            }
            catch
            {
                return Task.FromResult(string.Empty);
            }
        }

        // Treat identifier as a blob name in the configured (or provided) container
        var resolvedContainerName = string.IsNullOrWhiteSpace(containerName) ? _configuration["AzureStorage:ContainerName"] ?? "avatars" : containerName;
        var svc = new BlobServiceClient(_connectionString);
        var bClient = svc.GetBlobContainerClient(resolvedContainerName).GetBlobClient(contentIdentifier);
        return Task.FromResult(GenerateSas(bClient, resolvedContainerName, contentIdentifier));
    }

    private static string GenerateSas(BlobClient blobClient, string containerName, string blobName)
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
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
