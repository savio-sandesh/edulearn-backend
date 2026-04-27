using System.Text.Json;
using EduLearn.Assessment.API.Models;
using EduLearn.Assessment.API.Repositories;
using EduLearn.Shared;
using MassTransit;

namespace EduLearn.Assessment.API.Services;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _quizRepository;
    private readonly IPublishEndpoint? _publishEndpoint;

    public QuizService(IQuizRepository quizRepository, IPublishEndpoint? publishEndpoint = null)
    {
        _quizRepository = quizRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Quiz> CreateQuiz(Quiz quiz)
    {
        ValidateQuiz(quiz);
        quiz.CreatedAt = DateTime.UtcNow;
        await _quizRepository.AddQuiz(quiz);
        await _quizRepository.SaveChanges();
        return quiz;
    }

    public async Task<Quiz?> GetQuizById(int id)
    {
        return await _quizRepository.FindByQuizId(id);
    }

    public async Task<IReadOnlyList<Quiz>> GetQuizzesByCourse(int courseId)
    {
        return await _quizRepository.FindByCourseId(courseId);
    }

    public async Task<Quiz?> GetQuizByLesson(int lessonId)
    {
        return await _quizRepository.FindByLessonId(lessonId);
    }

    public async Task<Quiz?> UpdateQuiz(int id, Quiz quiz)
    {
        var existing = await _quizRepository.FindByQuizId(id);
        if (existing == null)
        {
            return null;
        }

        ValidateQuiz(quiz);

        existing.CourseId = quiz.CourseId;
        existing.LessonId = quiz.LessonId;
        existing.Title = quiz.Title;
        existing.Description = quiz.Description;
        existing.TimeLimitMinutes = quiz.TimeLimitMinutes;
        existing.PassingScore = quiz.PassingScore;
        existing.MaxAttempts = quiz.MaxAttempts;
        existing.QuestionsJson = quiz.QuestionsJson;

        await _quizRepository.SaveChanges();
        return existing;
    }

    public async Task<bool> DeleteQuiz(int id)
    {
        var quiz = await _quizRepository.FindByQuizId(id);
        if (quiz == null)
        {
            return false;
        }

        await _quizRepository.DeleteQuiz(quiz);
        await _quizRepository.SaveChanges();
        return true;
    }

    public async Task<bool> PublishQuiz(int id)
    {
        var quiz = await _quizRepository.FindByQuizId(id);
        if (quiz == null)
        {
            return false;
        }

        quiz.IsPublished = true;
        await _quizRepository.SaveChanges();
        return true;
    }

    public async Task<QuizAttempt> StartAttempt(int studentId, int quizId)
    {
        var quiz = await _quizRepository.FindByQuizId(quizId)
            ?? throw new ArgumentException("Quiz not found.");

        if (!quiz.IsPublished)
        {
            throw new InvalidOperationException("Quiz is not published yet.");
        }

        var attempts = await _quizRepository.CountAttempts(studentId, quizId);
        if (attempts >= quiz.MaxAttempts)
        {
            throw new InvalidOperationException("Maximum attempts reached for this quiz.");
        }

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = studentId,
            StartedAt = DateTime.UtcNow,
            Answers = "{}",
            Score = 0,
            IsPassed = false
        };

        await _quizRepository.AddAttempt(attempt);
        await _quizRepository.SaveChanges();
        return attempt;
    }

    public async Task<QuizAttempt?> SubmitAttempt(int attemptId, Dictionary<int, string> answers)
    {
        var attempt = await _quizRepository.FindAttemptById(attemptId);
        if (attempt == null)
        {
            return null;
        }

        if (attempt.SubmittedAt.HasValue)
        {
            throw new InvalidOperationException("Attempt was already submitted.");
        }

        var quiz = await _quizRepository.FindByQuizId(attempt.QuizId)
            ?? throw new InvalidOperationException("Quiz not found for attempt.");

        var wasCourseCompletedBefore = await IsCourseCompleted(attempt.StudentId, quiz.CourseId);

        var correctAnswers = DeserializeAnswers(quiz.QuestionsJson);
        var submittedAnswers = answers ?? new Dictionary<int, string>();
        var serializedAnswers = JsonSerializer.Serialize(submittedAnswers);
        var submittedAt = DateTime.UtcNow;

        var score = CalculateScore(submittedAnswers, correctAnswers);

        attempt.Answers = serializedAnswers;
        attempt.Score = score;
        attempt.IsPassed = score >= quiz.PassingScore;
        attempt.SubmittedAt = submittedAt;

        await _quizRepository.SaveChanges();

        if (attempt.IsPassed && !wasCourseCompletedBefore)
        {
            var isCourseCompletedNow = await IsCourseCompleted(attempt.StudentId, quiz.CourseId);
            if (isCourseCompletedNow && _publishEndpoint != null)
            {
                await _publishEndpoint.Publish<ICourseCompletedEvent>(new CourseCompletedEvent
                {
                    EnrollmentId = Guid.NewGuid(),
                    StudentId = attempt.StudentId,
                    CourseId = quiz.CourseId,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return attempt;
    }

    public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsByStudent(int studentId, int quizId)
    {
        return await _quizRepository.FindAttemptsByStudentAndQuiz(studentId, quizId);
    }

    public async Task<QuizAttempt?> GetBestAttempt(int studentId, int quizId)
    {
        return await _quizRepository.FindBestAttempt(studentId, quizId);
    }

    public async Task<int> GetAttemptCount(int studentId, int quizId)
    {
        return await _quizRepository.CountAttempts(studentId, quizId);
    }

    private static Dictionary<int, string> DeserializeAnswers(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<int, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<int, string>>(json)
            ?? new Dictionary<int, string>();
    }

    private static int CalculateScore(Dictionary<int, string> submitted, Dictionary<int, string> correct)
    {
        if (correct.Count == 0)
        {
            return 0;
        }

        var matched = 0;
        foreach (var item in correct)
        {
            if (submitted.TryGetValue(item.Key, out var provided) &&
                string.Equals(provided?.Trim(), item.Value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                matched++;
            }
        }

        return (int)Math.Round((matched / (double)correct.Count) * 100, MidpointRounding.AwayFromZero);
    }

    private static void ValidateQuiz(Quiz quiz)
    {
        if (quiz.PassingScore < 0 || quiz.PassingScore > 100)
        {
            throw new ArgumentException("PassingScore must be between 0 and 100.");
        }

        if (quiz.MaxAttempts <= 0)
        {
            throw new ArgumentException("MaxAttempts must be greater than zero.");
        }

        // Validate JSON shape early to prevent invalid scoring payloads.
        _ = DeserializeAnswers(quiz.QuestionsJson);
    }

    private async Task<bool> IsCourseCompleted(int studentId, int courseId)
    {
        var publishedQuizCount = await _quizRepository.CountPublishedQuizzesByCourse(courseId);
        if (publishedQuizCount == 0)
        {
            return false;
        }

        var passedQuizCount = await _quizRepository.CountDistinctPassedQuizzesByStudentForCourse(studentId, courseId);
        return passedQuizCount >= publishedQuizCount;
    }
}
