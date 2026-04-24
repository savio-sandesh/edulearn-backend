using System.Net.Http.Headers;

namespace EduLearn.Enrollment.API.Services;

public class CourseService : ICourseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CourseService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task IncrementEnrollmentAsync(int courseId)
    {
        var baseUrl = _configuration["CourseApi:BaseUrl"] ?? "http://localhost:5224";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var parsed))
        {
            client.DefaultRequestHeaders.Authorization = parsed;
        }

        using var response = await client.PostAsync($"/api/courses/{courseId}/enrollments/increment", content: null);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Course enrollment increment failed with status code {(int)response.StatusCode}.");
        }
    }
}
