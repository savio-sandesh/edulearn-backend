using EduLearn.Review.API.DTOs;

namespace EduLearn.Review.API.Services;

public interface IReviewService
{
    Task<ReviewResponseDto> AddReviewAsync(CreateReviewDto reviewDto, int studentId);
    Task<bool> ApproveReviewAsync(int reviewId);
    Task<IReadOnlyList<ReviewResponseDto>> GetApprovedReviewsByCourseAsync(int courseId);
    Task<double> GetAverageRatingAsync(int courseId);
}
