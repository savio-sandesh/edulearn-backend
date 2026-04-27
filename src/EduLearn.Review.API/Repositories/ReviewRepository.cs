using EduLearn.Review.API.Data;
using EduLearn.Review.API.Models;
using Microsoft.EntityFrameworkCore;
using ReviewEntity = EduLearn.Review.API.Models.Review;

namespace EduLearn.Review.API.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ReviewDbContext _dbContext;

    public ReviewRepository(ReviewDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasStudentReviewedAsync(int courseId, int studentId)
    {
        return await _dbContext.Reviews.AnyAsync(r => r.CourseId == courseId && r.StudentId == studentId);
    }

    public async Task AddReviewAsync(ReviewEntity review)
    {
        await _dbContext.Reviews.AddAsync(review);
    }

    public async Task<int> ApproveReviewAsync(int reviewId)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var review = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
            if (review == null || review.IsApproved)
            {
                return 0;
            }

            review.IsApproved = true;
            await _dbContext.SaveChangesAsync();
            return 1;
        }

        return await _dbContext.Reviews
            .Where(r => r.ReviewId == reviewId && !r.IsApproved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IsApproved, true));
    }

    public async Task<IReadOnlyList<ReviewEntity>> GetApprovedByCourseAsync(int courseId)
    {
        return await _dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<double> GetAverageRatingAsync(int courseId)
    {
        var approvedReviews = _dbContext.Reviews
            .Where(r => r.CourseId == courseId && r.IsApproved);

        if (!await approvedReviews.AnyAsync())
        {
            return 0d;
        }

        return await approvedReviews.AverageAsync(r => r.Rating);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
