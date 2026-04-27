namespace EduLearn.Progress.API.Services;

public interface ICertificateService
{
    Task<string> GenerateCertificateAsync(
        int studentId,
        int courseId,
        string verificationCode,
        DateTime issuedAtUtc,
        string studentName,
        string? avatarUrl = null);
}
