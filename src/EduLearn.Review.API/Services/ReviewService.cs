using EduLearn.Review.API.Data;
using EduLearn.Review.API.DTOs;
using EduLearn.Review.API.Models;
using EduLearn.Review.API.Repositories;
using Microsoft.EntityFrameworkCore;
using ReviewEntity = EduLearn.Review.API.Models.Review;

namespace EduLearn.Review.API.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IEnrollmentServiceClient _enrollmentServiceClient;
    private readonly ReviewDbContext _dbContext;

    public ReviewService(IReviewRepository reviewRepository, IEnrollmentServiceClient enrollmentServiceClient, ReviewDbContext dbContext)
    {
        _reviewRepository = reviewRepository;
        _enrollmentServiceClient = enrollmentServiceClient;
        _dbContext = dbContext;
    }

    public async Task<ReviewResponseDto> AddReviewAsync(CreateReviewDto reviewDto, int studentId)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Business Logic - Check enrollment
                var isEnrolled = await _enrollmentServiceClient.IsEnrolledAsync(reviewDto.CourseId);
                if (!isEnrolled)
                {
                    throw new InvalidOperationException("Only enrolled students can submit reviews.");
                }

                // 2. Check if student already reviewed
                var hasReviewed = await _reviewRepository.HasStudentReviewedAsync(reviewDto.CourseId, studentId);
                if (hasReviewed)
                {
                    throw new InvalidOperationException("You have already submitted a review for this course.");
                }

                // 3. Create review entity
                var review = new ReviewEntity
                {
                    CourseId = reviewDto.CourseId,
                    StudentId = studentId,
                    Rating = reviewDto.Rating,
                    Comment = reviewDto.Comment?.Trim(),
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                };

                // 4. Add and save to database
                try
                {
                    await _reviewRepository.AddReviewAsync(review);
                    await _reviewRepository.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw new InvalidOperationException("You have already submitted a review for this course.");
                }

                await transaction.CommitAsync();
                return MapToResponseDto(review);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> ApproveReviewAsync(int reviewId)
    {
        var affectedRows = await _reviewRepository.ApproveReviewAsync(reviewId);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<ReviewResponseDto>> GetApprovedReviewsByCourseAsync(int courseId)
    {
        var reviews = await _reviewRepository.GetApprovedByCourseAsync(courseId);
        return reviews.Select(MapToResponseDto).ToList();
    }

    public async Task<double> GetAverageRatingAsync(int courseId)
    {
        return await _reviewRepository.GetAverageRatingAsync(courseId);
    }

    private static ReviewResponseDto MapToResponseDto(ReviewEntity review)
    {
        return new ReviewResponseDto
        {
            ReviewId = review.ReviewId,
            CourseId = review.CourseId,
            StudentId = review.StudentId,
            Rating = review.Rating,
            Comment = review.Comment,
            IsApproved = review.IsApproved,
            CreatedAt = review.CreatedAt
        };
    }
}
