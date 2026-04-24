using EduLearn.Enrollment.API.DTOs;

namespace EduLearn.Enrollment.API.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto> EnrollAsync(int studentId, int courseId);
    Task<EnrollmentResponseDto?> GetEnrollmentByIdAsync(int enrollmentId);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetEnrollmentsByStudentAsync(int studentId);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetEnrollmentsByCourseAsync(int courseId);
    Task<bool> IsEnrolledAsync(int studentId, int courseId);
    Task<EnrollmentResponseDto?> UpdateProgressAsync(int enrollmentId);
    Task<bool> CompleteEnrollmentAsync(int studentId, int courseId);
    Task<bool> IssueCertificateAsync(int enrollmentId);
    Task<bool> DropCourseAsync(int studentId, int courseId);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetCompletedCoursesAsync(int studentId);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetInProgressCoursesAsync(int studentId);
    Task<int> GetEnrollmentCountAsync(int courseId);
}
