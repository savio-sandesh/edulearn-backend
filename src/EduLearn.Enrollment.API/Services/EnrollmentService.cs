using EduLearn.Enrollment.API.DTOs;
using EduLearn.Enrollment.API.Models;
using EduLearn.Enrollment.API.Repositories;

namespace EduLearn.Enrollment.API.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseService _courseService;
    private readonly IProgressService _progressService;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        ICourseService courseService,
        IProgressService progressService)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseService = courseService;
        _progressService = progressService;
    }

    public async Task<EnrollmentResponseDto> EnrollAsync(int studentId, int courseId)
    {
        if (await IsEnrolledAsync(studentId, courseId))
        {
            throw new ArgumentException("Student is already enrolled in this course.");
        }

        await using var transaction = await _enrollmentRepository.BeginTransactionAsync();

        var enrollment = new Models.Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.ACTIVE,
            ProgressPercent = 0,
            LastAccessedAt = DateTime.UtcNow
        };

        await _enrollmentRepository.AddAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        await _courseService.IncrementEnrollmentAsync(courseId);

        await transaction.CommitAsync();

        return MapToResponse(enrollment);
    }

    public async Task<EnrollmentResponseDto?> GetEnrollmentByIdAsync(int enrollmentId)
    {
        var enrollment = await _enrollmentRepository.FindByEnrollmentIdAsync(enrollmentId);
        return enrollment == null ? null : MapToResponse(enrollment);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetEnrollmentsByStudentAsync(int studentId)
    {
        var enrollments = await _enrollmentRepository.FindByStudentIdAsync(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetEnrollmentsByCourseAsync(int courseId)
    {
        var enrollments = await _enrollmentRepository.FindByCourseIdAsync(courseId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
    {
        return await _enrollmentRepository.IsEnrolledAsync(studentId, courseId);
    }

    public async Task<EnrollmentResponseDto?> UpdateProgressAsync(int enrollmentId)
    {
        var enrollment = await _enrollmentRepository.FindByEnrollmentIdAsync(enrollmentId);
        if (enrollment == null)
        {
            return null;
        }

        var progress = await _progressService.GetCourseProgressAsync(enrollment.StudentId, enrollment.CourseId);
        var total = Math.Max(progress.TotalLessons, 0);
        var completed = Math.Clamp(progress.CompletedLessons, 0, total == 0 ? 0 : total);

        enrollment.ProgressPercent = total == 0 ? 0 : (int)((completed / (double)total) * 100);
        enrollment.LastAccessedAt = DateTime.UtcNow;

        await _enrollmentRepository.UpdateAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();
        return MapToResponse(enrollment);
    }

    public async Task<bool> CompleteEnrollmentAsync(int studentId, int courseId)
    {
        var enrollment = await _enrollmentRepository.FindByStudentAndCourseAsync(studentId, courseId);
        if (enrollment == null || enrollment.Status != EnrollmentStatus.ACTIVE)
        {
            return false;
        }

        var progress = await _progressService.GetCourseProgressAsync(studentId, courseId);

        enrollment.Status = EnrollmentStatus.COMPLETED;
        enrollment.CompletedAt = DateTime.UtcNow;
        enrollment.ProgressPercent = 100;

        await _enrollmentRepository.UpdateAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        if (progress.AllQuizzesPassed)
        {
            await IssueCertificateAsync(enrollment.EnrollmentId);
        }

        return true;
    }

    public async Task<bool> IssueCertificateAsync(int enrollmentId)
    {
        var enrollment = await _enrollmentRepository.FindByEnrollmentIdAsync(enrollmentId);
        if (enrollment == null)
        {
            return false;
        }

        if (enrollment.Status != EnrollmentStatus.COMPLETED)
        {
            throw new InvalidOperationException("Certificate can only be issued for completed enrollments.");
        }

        enrollment.CertificateIssued = true;
        await _enrollmentRepository.UpdateAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DropCourseAsync(int studentId, int courseId)
    {
        var enrollment = await _enrollmentRepository.FindByStudentAndCourseAsync(studentId, courseId);
        if (enrollment == null || enrollment.Status != EnrollmentStatus.ACTIVE)
        {
            return false;
        }

        enrollment.Status = EnrollmentStatus.DROPPED;
        enrollment.LastAccessedAt = DateTime.UtcNow;
        await _enrollmentRepository.UpdateAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetCompletedCoursesAsync(int studentId)
    {
        var enrollments = await _enrollmentRepository.FindCompletedAsync(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetInProgressCoursesAsync(int studentId)
    {
        var enrollments = await _enrollmentRepository.FindInProgressAsync(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<int> GetEnrollmentCountAsync(int courseId)
    {
        return await _enrollmentRepository.CountByCourseIdAsync(courseId);
    }

    private static EnrollmentResponseDto MapToResponse(Models.Enrollment enrollment)
    {
        return new EnrollmentResponseDto
        {
            EnrollmentId = enrollment.EnrollmentId,
            StudentId = enrollment.StudentId,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            CompletedAt = enrollment.CompletedAt,
            Status = enrollment.Status,
            ProgressPercent = enrollment.ProgressPercent,
            LastAccessedAt = enrollment.LastAccessedAt,
            CertificateIssued = enrollment.CertificateIssued,
            PaymentId = enrollment.PaymentId
        };
    }
}
