namespace EduLearn.Content.API.Services;

public interface IBlobService
{
    Task<string> GenerateReadSasUrlAsync(string contentUrl);
}
