using EduLearn.Review.API.Data;
using EduLearn.Review.API.Models;
using EduLearn.Review.API.Repositories;
using Microsoft.EntityFrameworkCore;
using ReviewEntity = EduLearn.Review.API.Models.Review;

namespace EduLearn.Review.Tests;

public class ReviewRepositoryTests
{
    private static ReviewDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ReviewDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ReviewDbContext(options);
    }

    [Test]
    public async Task GetAverageRatingAsync_ReturnsMeanOfApprovedReviewsOnly()
    {
        // Arrange
        await using var context = CreateInMemoryContext(Guid.NewGuid().ToString("N"));
        await context.Reviews.AddRangeAsync(
            new ReviewEntity { CourseId = 7, StudentId = 1, Rating = 5, IsApproved = true, CreatedAt = DateTime.UtcNow },
            new ReviewEntity { CourseId = 7, StudentId = 2, Rating = 3, IsApproved = true, CreatedAt = DateTime.UtcNow },
            new ReviewEntity { CourseId = 7, StudentId = 3, Rating = 1, IsApproved = false, CreatedAt = DateTime.UtcNow },
            new ReviewEntity { CourseId = 8, StudentId = 4, Rating = 2, IsApproved = true, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ReviewRepository(context);

        // Act
        var average = await repository.GetAverageRatingAsync(7);

        // Assert
        Assert.That(average, Is.EqualTo(4d));
    }

    [Test]
    public async Task ApproveReviewAsync_SetsIsApprovedToTrue()
    {
        // Arrange
        await using var context = CreateInMemoryContext(Guid.NewGuid().ToString("N"));
        var review = new ReviewEntity
        {
            CourseId = 10,
            StudentId = 99,
            Rating = 4,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        await context.Reviews.AddAsync(review);
        await context.SaveChangesAsync();

        var repository = new ReviewRepository(context);

        // Act
        var rows = await repository.ApproveReviewAsync(review.ReviewId);

        // Assert
        Assert.That(rows, Is.EqualTo(1));

        var updated = await context.Reviews.SingleAsync(x => x.ReviewId == review.ReviewId);
        Assert.That(updated.IsApproved, Is.True);
    }
}
