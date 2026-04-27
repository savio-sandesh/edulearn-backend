using EduLearn.Progress.API.Data;
using EduLearn.Progress.API.Models;
using EduLearn.Progress.API.Services;
using EduLearn.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Progress.API.Consumers;

public class CourseCompletedConsumer : IConsumer<ICourseCompletedEvent>
{
    private readonly ProgressDbContext _dbContext;
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CourseCompletedConsumer> _logger;

    public CourseCompletedConsumer(
        ProgressDbContext dbContext,
        ICertificateService certificateService,
        ILogger<CourseCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _certificateService = certificateService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ICourseCompletedEvent> context)
    {
        var message = context.Message;

        var existingCertificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(x => x.StudentId == message.StudentId && x.CourseId == message.CourseId);

        if (existingCertificate != null)
        {
            _logger.LogInformation(
                "Certificate already exists for student {StudentId} and course {CourseId}",
                message.StudentId,
                message.CourseId);
            return;
        }

        var issueDate = DateTime.UtcNow;
        var verificationCode = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var certificateUrl = await _certificateService.GenerateCertificateAsync(
            message.StudentId,
            message.CourseId,
            verificationCode,
            issueDate,
            $"Student {message.StudentId}");

        var certificate = new Certificate
        {
            StudentId = message.StudentId,
            CourseId = message.CourseId,
            CertificateUrl = certificateUrl,
            IssuedAt = issueDate,
            VerificationCode = verificationCode
        };

        await _dbContext.Certificates.AddAsync(certificate);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Certificate issued for student {StudentId}, course {CourseId}, verification {VerificationCode}",
            message.StudentId,
            message.CourseId,
            verificationCode);
    }
}
