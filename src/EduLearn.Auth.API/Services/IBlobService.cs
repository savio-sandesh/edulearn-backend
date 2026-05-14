namespace EduLearn.Auth.API.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<string> GenerateReadSasUrlAsync(string fileName);
    }
}