namespace EduLearn.Content.API.Services;

public interface IBlobService
{
    Task<string> GenerateReadSasUrlAsync(string contentUrl);
    Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType, string containerName = "lesson-videos");
}
