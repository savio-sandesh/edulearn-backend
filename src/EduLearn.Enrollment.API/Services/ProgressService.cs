using System.Text.Json;

namespace EduLearn.Enrollment.API.Services;

public class ProgressService : IProgressService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ProgressService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<CourseProgressSnapshot> GetCourseProgressAsync(int studentId, int courseId)
    {
        var useMock = bool.TryParse(_configuration["Progress:UseMock"], out var parsed) && parsed;
        if (useMock)
        {
            return new CourseProgressSnapshot
            {
                CompletedLessons = int.TryParse(_configuration["Progress:MockCompletedLessons"], out var c) ? c : 0,
                TotalLessons = int.TryParse(_configuration["Progress:MockTotalLessons"], out var t) ? t : 0,
                AllQuizzesPassed = bool.TryParse(_configuration["Progress:MockAllQuizzesPassed"], out var q) && q
            };
        }

        var progressApiUrl = _configuration["ProgressApi:BaseUrl"] ?? "http://localhost:5218";
        
        try
        {
            var response = await _httpClient.GetAsync($"{progressApiUrl}/api/progress/lesson-progress?studentId={studentId}&courseId={courseId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var records = JsonSerializer.Deserialize<ProgressRecordDto[]>(content, options) ?? Array.Empty<ProgressRecordDto>();
                
                return new CourseProgressSnapshot
                {
                    TotalLessons = records.Length,
                    CompletedLessons = records.Count(x => x.IsCompleted),
                    AllQuizzesPassed = false // Assuming no quizzes logic yet
                };
            }
        }
        catch (Exception)
        {
            // Fallback to 0 if API is unreachable
        }

        return new CourseProgressSnapshot { CompletedLessons = 0, TotalLessons = 0, AllQuizzesPassed = false };
    }

    private class ProgressRecordDto
    {
        public bool IsCompleted { get; set; }
    }
}
