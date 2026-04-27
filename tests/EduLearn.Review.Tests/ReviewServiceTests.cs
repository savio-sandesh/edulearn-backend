using EduLearn.Review.API.DTOs;
using EduLearn.Review.API.Repositories;
using EduLearn.Review.API.Services;
using Moq;

namespace EduLearn.Review.Tests;

public class ReviewServiceTests
{
    private Mock<IReviewRepository> _reviewRepositoryMock = null!;
    private Mock<IEnrollmentServiceClient> _enrollmentClientMock = null!;
    private ReviewService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _reviewRepositoryMock = new Mock<IReviewRepository>();
        _enrollmentClientMock = new Mock<IEnrollmentServiceClient>();
        _service = new ReviewService(_reviewRepositoryMock.Object, _enrollmentClientMock.Object);
    }

    [Test]
    public void AddReviewAsync_WhenStudentIsNotEnrolled_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateReviewDto
        {
            CourseId = 100,
            Rating = 4,
            Comment = "Good"
        };

        _enrollmentClientMock
            .Setup(x => x.IsEnrolledAsync(request.CourseId))
            .ReturnsAsync(false);

        // Act + Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddReviewAsync(request, studentId: 42));

        Assert.That(ex!.Message, Is.EqualTo("Only enrolled students can submit reviews."));
        _reviewRepositoryMock.Verify(x => x.HasStudentReviewedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _reviewRepositoryMock.Verify(x => x.AddReviewAsync(It.IsAny<EduLearn.Review.API.Models.Review>()), Times.Never);
        _reviewRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public void AddReviewAsync_WhenDuplicateReviewExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateReviewDto
        {
            CourseId = 101,
            Rating = 5,
            Comment = "Excellent"
        };

        _enrollmentClientMock
            .Setup(x => x.IsEnrolledAsync(request.CourseId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(x => x.HasStudentReviewedAsync(request.CourseId, 42))
            .ReturnsAsync(true);

        // Act + Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddReviewAsync(request, studentId: 42));

        Assert.That(ex!.Message, Is.EqualTo("You have already submitted a review for this course."));
        _reviewRepositoryMock.Verify(x => x.HasStudentReviewedAsync(request.CourseId, 42), Times.Once);
        _reviewRepositoryMock.Verify(x => x.AddReviewAsync(It.IsAny<EduLearn.Review.API.Models.Review>()), Times.Never);
        _reviewRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
