using EduLearn.Progress.API.Data;
using EduLearn.Progress.API.Models;
using EduLearn.Progress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Progress.API.Controllers;

[ApiController]
[Route("api/progress/debug")]
[AllowAnonymous]
public class CertificateDebugController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly ProgressDbContext _dbContext;

    public CertificateDebugController(ICertificateService certificateService, ProgressDbContext dbContext)
    {
        _certificateService = certificateService;
        _dbContext = dbContext;
    }

    [HttpPost("generate-certificate")]
    public async Task<IActionResult> GenerateCertificate(
        [FromQuery] int studentId = 999,
        [FromQuery] int courseId = 999,
        [FromQuery] string studentName = "Debug Student",
        [FromQuery] string? avatarUrl = null)
    {
        var issuedAt = DateTime.UtcNow;
        var verificationCode = $"DEBUG-{Guid.NewGuid():N}".ToUpperInvariant();

        var certificateUrl = await _certificateService.GenerateCertificateAsync(
            studentId,
            courseId,
            verificationCode,
            issuedAt,
            studentName,
            avatarUrl);

        var existingCertificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId);
        if (existingCertificate != null)
        {
            _dbContext.Certificates.Remove(existingCertificate);
        }

        var certificate = new Certificate
        {
            StudentId = studentId,
            CourseId = courseId,
            CertificateUrl = certificateUrl,
            IssuedAt = issuedAt,
            VerificationCode = verificationCode
        };

        await _dbContext.Certificates.AddAsync(certificate);

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Certificate PDF generated and persisted.",
            studentId,
            courseId,
            issuedAt,
            verificationCode,
            certificateUrl
        });
    }
}
