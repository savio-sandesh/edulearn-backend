using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduLearn.Progress.API.Services;

public class CertificateService : ICertificateService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CertificateService> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateCertificateAsync(
        int studentId,
        int courseId,
        string verificationCode,
        DateTime issuedAtUtc,
        string studentName,
        string? avatarUrl = null)
    {
        var configuredDirectory = _configuration["Certificate:OutputDirectory"];
        var certificatesDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? @"C:\Temp"
            : configuredDirectory;

        if (!Path.IsPathRooted(certificatesDirectory))
        {
            certificatesDirectory = Path.Combine(_environment.ContentRootPath, certificatesDirectory);
        }

        Directory.CreateDirectory(certificatesDirectory);

        var fileName = $"cert-course-{courseId}-student-{studentId}-{verificationCode}.pdf";
        var filePath = Path.Combine(certificatesDirectory, fileName);
        var avatarBytes = await GetAvatarBytesAsync(studentId, avatarUrl);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(13));

                page.Content().Border(1).Padding(20).Column(column =>
                {
                    column.Spacing(10);

                    if (avatarBytes.Length > 0)
                    {
                        column.Item().AlignCenter().Width(82).Height(82).Border(1).Padding(4).Image(avatarBytes).FitArea();
                    }

                    column.Item().PaddingTop(8).AlignCenter().Text("Certificate of Completion").Bold().FontSize(30);
                    column.Item().AlignCenter().Text(studentName).Bold().FontSize(24);
                    column.Item().AlignCenter().Text($"Student ID: {studentId}");
                    column.Item().AlignCenter().Text($"Course ID: {courseId}");
                    column.Item().AlignCenter().Text($"Issued (UTC): {issuedAtUtc:yyyy-MM-dd HH:mm:ss}");
                    column.Item().PaddingTop(12).AlignCenter().Text("This certifies successful completion of the course requirements.");
                    column.Item().PaddingTop(20).AlignCenter().Text($"Verification Code: {verificationCode}").Bold();
                });
            });
        }).GeneratePdf(filePath);

        var certificateUrl = $"/certificates/{fileName}";
        return certificateUrl;
    }

    private async Task<byte[]> GetAvatarBytesAsync(int studentId, string? avatarUrl)
    {
        var resolvedAvatarUrl = ResolveAvatarUrl(studentId, avatarUrl);
        if (!string.IsNullOrWhiteSpace(resolvedAvatarUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(resolvedAvatarUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                _logger.LogWarning("Avatar download failed with status {StatusCode} for URL {AvatarUrl}", response.StatusCode, resolvedAvatarUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Avatar download failed for URL {AvatarUrl}", resolvedAvatarUrl);
            }
        }

        var defaultAvatarPath = Path.Combine(GetWebRootPath(), "images", "default-avatar.png");
        if (File.Exists(defaultAvatarPath))
        {
            return await File.ReadAllBytesAsync(defaultAvatarPath);
        }

        _logger.LogWarning("Default avatar not found at {DefaultAvatarPath}", defaultAvatarPath);
        return Array.Empty<byte>();
    }

    private string ResolveAvatarUrl(int studentId, string? avatarUrl)
    {
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            return avatarUrl;
        }

        var template = _configuration["Avatar:BlobUrlTemplate"];
        if (!string.IsNullOrWhiteSpace(template))
        {
            return template.Replace("{studentId}", studentId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Empty;
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            return _environment.WebRootPath;
        }

        return Path.Combine(_environment.ContentRootPath, "wwwroot");
    }
}
