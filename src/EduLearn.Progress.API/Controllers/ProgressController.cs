using EduLearn.Progress.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Progress.API.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize(Roles = "STUDENT,INSTRUCTOR,ADMIN")]
public class ProgressController : ControllerBase
{
    private readonly ProgressDbContext _dbContext;

    public ProgressController(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("mark-complete")]
    public async Task<IActionResult> MarkComplete([FromBody] MarkCompleteRequest request)
    {
        if (request.StudentId <= 0 || request.CourseId <= 0 || request.LessonId <= 0)
        {
            return BadRequest(new { message = "StudentId, CourseId, and LessonId must be positive integers." });
        }

        var record = await _dbContext.LessonProgress
            .FirstOrDefaultAsync(x =>
                x.StudentId == request.StudentId &&
                x.CourseId == request.CourseId &&
                x.LessonId == request.LessonId);

        if (record == null)
        {
            record = new()
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                LessonId = request.LessonId,
                IsCompleted = request.IsCompleted,
                CompletedAt = request.IsCompleted ? DateTime.UtcNow : null
            };

            await _dbContext.LessonProgress.AddAsync(record);
        }
        else
        {
            record.IsCompleted = request.IsCompleted;
            record.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
        }

        await _dbContext.SaveChangesAsync();

        var courseProgressRecords = await _dbContext.LessonProgress
            .Where(x => x.StudentId == request.StudentId && x.CourseId == request.CourseId)
            .ToListAsync();

        var totalLessons = courseProgressRecords.Count;
        var completedLessons = courseProgressRecords.Count(x => x.IsCompleted);
        var progressPercent = totalLessons == 0
            ? 0m
            : Math.Round((completedLessons * 100m) / totalLessons, 2, MidpointRounding.AwayFromZero);

        foreach (var item in courseProgressRecords)
        {
            item.ProgressPercent = progressPercent;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Lesson progress upserted.",
            request.StudentId,
            request.CourseId,
            request.LessonId,
            request.IsCompleted,
            completedAt = record.CompletedAt,
            progressPercent,
            completedLessons,
            totalLessons
        });
    }

    [HttpGet("lesson-progress")]
    public async Task<IActionResult> GetLessonProgress([FromQuery] int? studentId, [FromQuery] int? courseId)
    {
        var query = _dbContext.LessonProgress.AsNoTracking().AsQueryable();

        if (studentId.HasValue)
        {
            query = query.Where(x => x.StudentId == studentId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(x => x.CourseId == courseId.Value);
        }

        var results = await query
            .OrderBy(x => x.StudentId)
            .ThenBy(x => x.CourseId)
            .ThenBy(x => x.LessonId)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("lesson-progress/{id:int}")]
    public async Task<IActionResult> GetLessonProgressById(int id)
    {
        var record = await _dbContext.LessonProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return record == null
            ? NotFound(new { message = "Lesson progress record not found." })
            : Ok(record);
    }

    [HttpGet("certificates")]
    public async Task<IActionResult> GetCertificates([FromQuery] int? studentId, [FromQuery] int? courseId)
    {
        var query = _dbContext.Certificates.AsNoTracking().AsQueryable();

        if (studentId.HasValue)
        {
            query = query.Where(x => x.StudentId == studentId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(x => x.CourseId == courseId.Value);
        }

        var results = await query
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("certificates/{id:int}")]
    public async Task<IActionResult> GetCertificateById(int id)
    {
        var record = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return record == null
            ? NotFound(new { message = "Certificate record not found." })
            : Ok(record);
    }

    [HttpGet("certificates/verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCertificate(string code)
    {
        if (!Guid.TryParse(code, out var certificateGuid))
        {
            return BadRequest(new { message = "Verification code must be a valid GUID." });
        }

        var normalizedCode = certificateGuid.ToString("N").ToUpperInvariant();
        var dashedCode = certificateGuid.ToString("D").ToUpperInvariant();

        var record = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VerificationCode == normalizedCode || x.VerificationCode == dashedCode);

        if (record == null)
        {
            return NotFound(new
            {
                valid = false,
                message = "Certificate not found for provided verification code."
            });
        }

        return Ok(new
        {
            valid = true,
            record.Id,
            record.StudentId,
            record.CourseId,
            record.IssuedAt,
            record.CertificateUrl,
            verificationCode = record.VerificationCode
        });
    }

    [HttpGet("certificates/download/{id:int}")]
    public async Task<IActionResult> DownloadCertificate(int id)
    {
        var record = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (record == null)
        {
            return NotFound(new { message = "Certificate record not found." });
        }

        if (string.IsNullOrWhiteSpace(record.CertificateUrl))
        {
            return BadRequest(new { message = "Certificate URL is missing." });
        }

        return Redirect(record.CertificateUrl);
    }

    public sealed class MarkCompleteRequest
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int LessonId { get; set; }
        public bool IsCompleted { get; set; }
    }
}
