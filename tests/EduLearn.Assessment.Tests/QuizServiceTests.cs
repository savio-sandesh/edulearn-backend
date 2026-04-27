using System.Text.Json;
using EduLearn.Assessment.API.Models;
using EduLearn.Assessment.API.Repositories;
using EduLearn.Assessment.API.Services;
using Moq;

namespace EduLearn.Assessment.Tests;

public class QuizServiceTests
{
    private Mock<IQuizRepository> _quizRepositoryMock = null!;
    private QuizService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _quizRepositoryMock = new Mock<IQuizRepository>();
        _service = new QuizService(_quizRepositoryMock.Object);
    }

    [Test]
    public async Task StartAttempt_WhenWithinLimit_CreatesAttempt()
    {
        // Arrange
        const int studentId = 1001;
        const int quizId = 11;

        var quiz = new Quiz
        {
            QuizId = quizId,
            IsPublished = true,
            MaxAttempts = 3,
            QuestionsJson = "{}"
        };

        QuizAttempt? addedAttempt = null;

        _quizRepositoryMock
            .Setup(x => x.FindByQuizId(quizId))
            .ReturnsAsync(quiz);

        _quizRepositoryMock
            .Setup(x => x.CountAttempts(studentId, quizId))
            .ReturnsAsync(1);

        _quizRepositoryMock
            .Setup(x => x.AddAttempt(It.IsAny<QuizAttempt>()))
            .Callback<QuizAttempt>(a => addedAttempt = a)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartAttempt(studentId, quizId);

        // Assert
        Assert.That(result.QuizId, Is.EqualTo(quizId));
        Assert.That(result.StudentId, Is.EqualTo(studentId));
        Assert.That(result.IsPassed, Is.False);
        Assert.That(result.Score, Is.EqualTo(0));
        Assert.That(result.Answers, Is.EqualTo("{}"));

        _quizRepositoryMock.Verify(x => x.AddAttempt(It.IsAny<QuizAttempt>()), Times.Once);
        _quizRepositoryMock.Verify(x => x.SaveChanges(), Times.Once);

        Assert.That(addedAttempt, Is.Not.Null);
        Assert.That(addedAttempt!.QuizId, Is.EqualTo(quizId));
        Assert.That(addedAttempt.StudentId, Is.EqualTo(studentId));
    }

    [Test]
    public void StartAttempt_WhenMaxAttemptsReached_ThrowsInvalidOperationException()
    {
        // Arrange
        const int studentId = 1001;
        const int quizId = 12;

        var quiz = new Quiz
        {
            QuizId = quizId,
            IsPublished = true,
            MaxAttempts = 2,
            QuestionsJson = "{}"
        };

        _quizRepositoryMock
            .Setup(x => x.FindByQuizId(quizId))
            .ReturnsAsync(quiz);

        _quizRepositoryMock
            .Setup(x => x.CountAttempts(studentId, quizId))
            .ReturnsAsync(2);

        // Act + Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.StartAttempt(studentId, quizId));

        _quizRepositoryMock.Verify(x => x.AddAttempt(It.IsAny<QuizAttempt>()), Times.Never);
        _quizRepositoryMock.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Test]
    public async Task SubmitAttempt_WhenScoreMeetsPassingScore_SetsPassedTrueAndPersistsAnswers()
    {
        // Arrange
        const int attemptId = 200;

        var quiz = new Quiz
        {
            QuizId = 20,
            PassingScore = 60,
            QuestionsJson = JsonSerializer.Serialize(new Dictionary<int, string>
            {
                [1] = "A",
                [2] = "B",
                [3] = "C",
                [4] = "D",
                [5] = "A"
            })
        };

        var attempt = new QuizAttempt
        {
            AttemptId = attemptId,
            QuizId = 20,
            StudentId = 900,
            StartedAt = DateTime.UtcNow
        };

        var submittedAnswers = new Dictionary<int, string>
        {
            [1] = "A",
            [2] = "B",
            [3] = "X",
            [4] = "D",
            [5] = "A"
        };

        _quizRepositoryMock
            .Setup(x => x.FindAttemptById(attemptId))
            .ReturnsAsync(attempt);

        _quizRepositoryMock
            .Setup(x => x.FindByQuizId(quiz.QuizId))
            .ReturnsAsync(quiz);

        // Act
        var result = await _service.SubmitAttempt(attemptId, submittedAnswers);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Score, Is.EqualTo(80));
        Assert.That(result.IsPassed, Is.True);
        Assert.That(result.SubmittedAt, Is.Not.Null);

        var persistedAnswers = JsonSerializer.Deserialize<Dictionary<int, string>>(result.Answers);
        Assert.That(persistedAnswers, Is.Not.Null);
        Assert.That(persistedAnswers![1], Is.EqualTo("A"));
        Assert.That(persistedAnswers[3], Is.EqualTo("X"));

        _quizRepositoryMock.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Test]
    public async Task SubmitAttempt_WhenScoreBelowPassingScore_SetsPassedFalse()
    {
        // Arrange
        const int attemptId = 201;

        var quiz = new Quiz
        {
            QuizId = 21,
            PassingScore = 75,
            QuestionsJson = JsonSerializer.Serialize(new Dictionary<int, string>
            {
                [1] = "A",
                [2] = "B",
                [3] = "C",
                [4] = "D"
            })
        };

        var attempt = new QuizAttempt
        {
            AttemptId = attemptId,
            QuizId = 21,
            StudentId = 901,
            StartedAt = DateTime.UtcNow
        };

        var submittedAnswers = new Dictionary<int, string>
        {
            [1] = "A",
            [2] = "X",
            [3] = "X",
            [4] = "D"
        };

        _quizRepositoryMock
            .Setup(x => x.FindAttemptById(attemptId))
            .ReturnsAsync(attempt);

        _quizRepositoryMock
            .Setup(x => x.FindByQuizId(quiz.QuizId))
            .ReturnsAsync(quiz);

        // Act
        var result = await _service.SubmitAttempt(attemptId, submittedAnswers);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Score, Is.EqualTo(50));
        Assert.That(result.IsPassed, Is.False);
        Assert.That(result.SubmittedAt, Is.Not.Null);

        _quizRepositoryMock.Verify(x => x.SaveChanges(), Times.Once);
    }
}
