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

    public ReviewService(IReviewRepository reviewRepository, IEnrollmentServiceClient enrollmentServiceClient)
    {
        _reviewRepository = reviewRepository;
        _enrollmentServiceClient = enrollmentServiceClient;
    }

    public async Task<ReviewResponseDto> AddReviewAsync(CreateReviewDto reviewDto, int studentId)
    {
        var isEnrolled = await _enrollmentServiceClient.IsEnrolledAsync(reviewDto.CourseId);
        if (!isEnrolled)
        {
            throw new InvalidOperationException("Only enrolled students can submit reviews.");
        }

        var hasReviewed = await _reviewRepository.HasStudentReviewedAsync(reviewDto.CourseId, studentId);
        if (hasReviewed)
        {
            throw new InvalidOperationException("You have already submitted a review for this course.");
        }

        var review = new ReviewEntity
        {
            CourseId = reviewDto.CourseId,
            StudentId = studentId,
            Rating = reviewDto.Rating,
            Comment = reviewDto.Comment?.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _reviewRepository.AddReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("You have already submitted a review for this course.");
        }

        return MapToResponseDto(review);
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
