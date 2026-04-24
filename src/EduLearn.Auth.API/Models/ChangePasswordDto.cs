using System.ComponentModel.DataAnnotations;

namespace EduLearn.Auth.API.Models
{
    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
