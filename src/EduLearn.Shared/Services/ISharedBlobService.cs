using System.IO;

namespace EduLearn.Shared.Services;

public interface ISharedBlobService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? containerName = null);
    Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType, string? containerName = null);
    Task<string> GenerateReadSasUrlAsync(string contentIdentifier, string? containerName = null);
}
