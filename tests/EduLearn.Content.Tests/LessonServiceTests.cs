using EduLearn.Content.API.DTOs;
using EduLearn.Content.API.Models;
using EduLearn.Content.API.Repositories;
using EduLearn.Content.API.Services;
using Moq;
using ContentLesson = EduLearn.Content.API.Models.Lesson;

namespace EduLearn.Content.Tests;

public class LessonServiceTests
{
    private Mock<ILessonRepository> _lessonRepositoryMock = null!;
    private Mock<IBlobService> _blobServiceMock = null!;
    private LessonService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _lessonRepositoryMock = new Mock<ILessonRepository>();
        _blobServiceMock = new Mock<IBlobService>();
        _blobServiceMock
            .Setup(x => x.GenerateReadSasUrlAsync(It.IsAny<string>()))
            .ReturnsAsync((string contentUrl) => contentUrl);

        _service = new LessonService(_lessonRepositoryMock.Object, _blobServiceMock.Object);
    }

    [Test]
    public async Task GetLessonsByCourseAsync_WhenCourseHasLessons_ReturnsMappedList()
    {
        // Arrange
        const int courseId = 25;
        var lessons = new List<ContentLesson>
        {
            new()
            {
                LessonId = 1,
                CourseId = courseId,
                Title = "Intro",
                Description = "Basics",
                ContentType = "VIDEO",
                ContentUrl = "https://cdn.test/intro.mp4",
                DurationMinutes = 10,
                DisplayOrder = 1,
                IsPreview = true,
                IsPublished = true
            },
            new()
            {
                LessonId = 2,
                CourseId = courseId,
                Title = "Deep Dive",
                Description = "Premium",
                ContentType = "ARTICLE",
                ContentUrl = "https://cdn.test/deep-dive",
                DurationMinutes = 20,
                DisplayOrder = 2,
                IsPreview = false,
                IsPublished = true
            }
        };

        _lessonRepositoryMock
            .Setup(x => x.FindByCourseIdAsync(courseId))
            .ReturnsAsync(lessons);

        // Act
        var result = await _service.GetLessonsByCourseAsync(courseId);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].LessonId, Is.EqualTo(1));
        Assert.That(result[1].LessonId, Is.EqualTo(2));
        _lessonRepositoryMock.Verify(x => x.FindByCourseIdAsync(courseId), Times.Once);
    }

    [Test]
    public async Task GetLessonByIdAsync_WhenLessonIsPreview_ReturnsPreviewData()
    {
        // Arrange
        const int lessonId = 11;
        var lesson = new ContentLesson
        {
            LessonId = lessonId,
            CourseId = 7,
            Title = "Preview Lesson",
            Description = "Accessible preview",
            ContentType = "VIDEO",
            ContentUrl = "https://cdn.test/preview.mp4",
            DurationMinutes = 5,
            DisplayOrder = 1,
            IsPreview = true,
            IsPublished = true
        };

        _lessonRepositoryMock
            .Setup(x => x.FindByLessonIdAsync(lessonId))
            .ReturnsAsync(lesson);

        // Act
        var result = await _service.GetLessonByIdAsync(lessonId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LessonId, Is.EqualTo(lessonId));
        Assert.That(result.IsPreview, Is.True);
        Assert.That(result.ContentUrl, Is.EqualTo(lesson.ContentUrl));
        _lessonRepositoryMock.Verify(x => x.FindByLessonIdAsync(lessonId), Times.Once);
    }

    [Test]
    public async Task IsPremiumLessonAsync_WhenLessonIsNotPreview_ReturnsTrue()
    {
        // Arrange
        const int lessonId = 22;
        var lesson = new ContentLesson
        {
            LessonId = lessonId,
            CourseId = 7,
            Title = "Premium Lesson",
            Description = "Paid content",
            ContentType = "ARTICLE",
            ContentUrl = "https://cdn.test/premium",
            DurationMinutes = 15,
            DisplayOrder = 2,
            IsPreview = false,
            IsPublished = true
        };

        _lessonRepositoryMock
            .Setup(x => x.FindByLessonIdAsync(lessonId))
            .ReturnsAsync(lesson);

        // Act
        var result = await _service.IsPremiumLessonAsync(lessonId);

        // Assert
        Assert.That(result, Is.True);
        _lessonRepositoryMock.Verify(x => x.FindByLessonIdAsync(lessonId), Times.Once);
    }

    [Test]
    public async Task CompleteLessonAsync_WhenLessonExists_UpdatesRepositoryStatus()
    {
        // Arrange
        const int lessonId = 33;
        var lesson = new ContentLesson
        {
            LessonId = lessonId,
            CourseId = 7,
            Title = "Lesson",
            Description = "Completion flow",
            ContentType = "PDF",
            ContentUrl = "https://cdn.test/lesson.pdf",
            DurationMinutes = 12,
            DisplayOrder = 1,
            IsPreview = false,
            IsPublished = true,
            IsCompleted = false
        };

        ContentLesson? updatedLesson = null;

        _lessonRepositoryMock
            .Setup(x => x.FindByLessonIdAsync(lessonId))
            .ReturnsAsync(lesson);

        _lessonRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ContentLesson>()))
            .Callback<ContentLesson>(x => updatedLesson = x)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CompleteLessonAsync(lessonId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(lesson.IsCompleted, Is.True);
        Assert.That(updatedLesson, Is.Not.Null);
        Assert.That(updatedLesson!.IsCompleted, Is.True);
        _lessonRepositoryMock.Verify(x => x.FindByLessonIdAsync(lessonId), Times.Once);
        _lessonRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ContentLesson>()), Times.Once);
        _lessonRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
