namespace EduLearn.Progress.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadCertificateAsync(Stream fileStream, string fileName, string contentType);
}
