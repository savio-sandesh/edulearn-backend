namespace EduLearn.Enrollment.API.Services;

public class ProgressService : IProgressService
{
    private readonly IConfiguration _configuration;

    public ProgressService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<CourseProgressSnapshot> GetCourseProgressAsync(int studentId, int courseId)
    {
        var useMock = bool.TryParse(_configuration["Progress:UseMock"], out var parsed) && parsed;
        if (!useMock)
        {
            // Placeholder for future Content API / message-bus integration.
            throw new NotSupportedException("Progress provider integration is not configured yet.");
        }

        var completed = ReadInt("Progress:MockCompletedLessons", 0);
        var total = ReadInt("Progress:MockTotalLessons", 0);
        var quizzesPassed = bool.TryParse(_configuration["Progress:MockAllQuizzesPassed"], out var allPassed) && allPassed;

        return Task.FromResult(new CourseProgressSnapshot
        {
            CompletedLessons = completed,
            TotalLessons = total,
            AllQuizzesPassed = quizzesPassed
        });
    }

    private int ReadInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) ? value : fallback;
    }
}
