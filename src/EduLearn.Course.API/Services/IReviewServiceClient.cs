namespace EduLearn.Course.API.Services;

public interface IReviewServiceClient
{
    Task<double> GetAverageRatingAsync(int courseId);
}
