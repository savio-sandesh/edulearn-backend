namespace EduLearn.Auth.API.Models
{
    /// <summary>
    /// Represents issued access/refresh tokens for auth workflows.
    /// </summary>
    public class AuthTokensResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
    }
}
