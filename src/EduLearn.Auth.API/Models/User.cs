using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EduLearn.Auth.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        
        // Roles: STUDENT, INSTRUCTOR, ADMIN
        [Required]
        [RegularExpression("^(STUDENT|INSTRUCTOR|ADMIN)$", ErrorMessage = "Role must be STUDENT, INSTRUCTOR, or ADMIN.")]
        public string Role { get; set; } = "STUDENT"; 
        
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}