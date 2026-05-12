using EduLearn.Course.API.DTOs;
using EduLearn.Course.API.Models;
using EduLearn.Course.API.Repositories;
using EduLearn.Course.API.Services;
using Moq;
using CourseModel = EduLearn.Course.API.Models.Course;

namespace EduLearn.Course.Tests;

public class CourseServiceTests
{
    private Mock<ICourseRepository> _courseRepositoryMock = null!;
    private Mock<IReviewServiceClient> _reviewServiceClientMock = null!;
    private CourseService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _reviewServiceClientMock = new Mock<IReviewServiceClient>();
        _service = new CourseService(_courseRepositoryMock.Object, _reviewServiceClientMock.Object);
    }

    [Test]
    public async Task IncrementEnrollmentAsync_WhenCourseExists_IncreasesCountAndCallsUpdate()
    {
        // Arrange
        const int courseId = 10;
        var course = new CourseModel
        {
            CourseId = courseId,
            EnrollmentCount = 5
        };

        CourseModel? updatedCourse = null;

        _courseRepositoryMock
            .Setup(x => x.FindByCourseIdAsync(courseId))
            .ReturnsAsync(course);

        _courseRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CourseModel>()))
            .Callback<CourseModel>(c => updatedCourse = c)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IncrementEnrollmentAsync(courseId);

        // Assert
        Assert.That(result, Is.True);
        _courseRepositoryMock.Verify(x => x.FindByCourseIdAsync(courseId), Times.Once);
        _courseRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<CourseModel>()), Times.Once);
        _courseRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        Assert.That(updatedCourse, Is.Not.Null);
        Assert.That(updatedCourse!.EnrollmentCount, Is.EqualTo(6));
    }

    [Test]
    public async Task CreateCourseAsync_WhenTitleIsNullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var invalidCourse = new CourseCreateDto
        {
            Title = null!,
            Description = "Desc",
            Category = "Development",
            Level = "BEGINNER",
            Language = "English",
            Price = 0,
            TotalDuration = 30
        };

        // Act
        var act = async () => await _service.CreateCourseAsync(invalidCourse, currentUserId: 1);

        // Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await act());
        _courseRepositoryMock.Verify(x => x.AddAsync(It.IsAny<CourseModel>()), Times.Never);
    }

    [Test]
    public async Task PublishCourseAsync_WhenCourseHasZeroLessons_DoesNotPublish()
    {
        // Arrange
        const int courseId = 30;
        const int instructorId = 7;

        var course = new CourseModel
        {
            CourseId = courseId,
            InstructorId = instructorId,
            TotalDuration = 0,
            IsPublished = false
        };

        _courseRepositoryMock
            .Setup(x => x.FindByCourseIdAsync(courseId))
            .ReturnsAsync(course);

        // Act
        var result = await _service.PublishCourseAsync(courseId, instructorId, isAdmin: false);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(course.IsPublished, Is.False);
        _courseRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
