namespace EduLearn.Review.API.Services;

public interface IEnrollmentServiceClient
{
    Task<bool> IsEnrolledAsync(int courseId);
}
