using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EduLearn.Enrollment.API.Services;

public class ProgressService : IProgressService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProgressService(IConfiguration configuration, HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
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
        var contentApiUrl = _configuration["ContentApi:BaseUrl"] ?? "http://localhost:5176";
        
        var request = _httpContextAccessor.HttpContext?.Request;
        var token = request?.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            int totalLessons = 0;
            var countResponse = await _httpClient.GetAsync($"{contentApiUrl}/api/lessons/count/{courseId}");
            if (countResponse.IsSuccessStatusCode)
            {
                var countContent = await countResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(countContent);
                if (doc.RootElement.TryGetProperty("count", out var countEl) && countEl.TryGetInt32(out var countVal))
                {
                    totalLessons = countVal;
                }
            }

            var response = await _httpClient.GetAsync($"{progressApiUrl}/api/progress/lesson-progress?studentId={studentId}&courseId={courseId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var records = JsonSerializer.Deserialize<ProgressRecordDto[]>(content, options) ?? Array.Empty<ProgressRecordDto>();
                
                return new CourseProgressSnapshot
                {
                    TotalLessons = totalLessons,
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
