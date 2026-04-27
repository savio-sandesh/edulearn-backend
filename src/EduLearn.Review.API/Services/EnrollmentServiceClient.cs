using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduLearn.Review.API.DTOs;

namespace EduLearn.Review.API.Services;

public class EnrollmentServiceClient : IEnrollmentServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public EnrollmentServiceClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<bool> IsEnrolledAsync(int courseId)
    {
        var baseUrl = _configuration["EnrollmentApi:BaseUrl"] ?? "http://localhost:5114";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var parsed))
        {
            client.DefaultRequestHeaders.Authorization = parsed;
        }

        using var response = await client.GetAsync($"/api/enrollments/isEnrolled/{courseId}");
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Enrollment check failed with status code {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<EnrollmentStatusDto>();
        return payload?.IsEnrolled ?? false;
    }
}
