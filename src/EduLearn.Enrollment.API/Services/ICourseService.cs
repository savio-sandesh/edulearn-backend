namespace EduLearn.Enrollment.API.Services;

public interface ICourseService
{
    Task IncrementEnrollmentAsync(int courseId);
}
