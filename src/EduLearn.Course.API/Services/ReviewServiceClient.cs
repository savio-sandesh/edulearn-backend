using System.Net.Http.Headers;

namespace EduLearn.Course.API.Services;

public class ReviewServiceClient : IReviewServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ReviewServiceClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<double> GetAverageRatingAsync(int courseId)
    {
        var baseUrl = _configuration["ReviewApi:BaseUrl"] ?? "http://localhost:5144";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        using var response = await client.GetAsync($"/api/reviews/course/{courseId}/average");
        if (!response.IsSuccessStatusCode)
        {
            // If Review API is down or endpoint doesn't exist, return 0
            return 0d;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (double.TryParse(content.Trim('"'), out var rating))
        {
            return rating;
        }

        return 0d;
    }
}
