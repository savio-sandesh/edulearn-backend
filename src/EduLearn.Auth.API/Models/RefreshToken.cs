namespace EduLearn.Auth.API.Models
{
    /// <summary>
    /// Represents a persisted refresh token (stored as hash) for JWT rotation flow.
    /// </summary>
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }
        public int UserId { get; set; }

        // Store only hash, never the raw token.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public User User { get; set; } = null!;
    }
}
