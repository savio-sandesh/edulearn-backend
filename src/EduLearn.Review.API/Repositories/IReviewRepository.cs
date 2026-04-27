using EduLearn.Review.API.Models;
using ReviewEntity = EduLearn.Review.API.Models.Review;

namespace EduLearn.Review.API.Repositories;

public interface IReviewRepository
{
    Task<bool> HasStudentReviewedAsync(int courseId, int studentId);
    Task AddReviewAsync(ReviewEntity review);
    Task<int> ApproveReviewAsync(int reviewId);
    Task<IReadOnlyList<ReviewEntity>> GetApprovedByCourseAsync(int courseId);
    Task<double> GetAverageRatingAsync(int courseId);
    Task SaveChangesAsync();
}
