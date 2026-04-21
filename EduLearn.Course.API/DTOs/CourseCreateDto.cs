using System.ComponentModel.DataAnnotations;

namespace EduLearn.Course.API.DTOs
{
    public class CourseCreateDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(BEGINNER|INTERMEDIATE|ADVANCED)$", ErrorMessage = "Level must be BEGINNER, INTERMEDIATE, or ADVANCED.")]
        public string Level { get; set; } = "BEGINNER";

        [Required]
        public string Language { get; set; } = "English";

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ThumbnailUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalDuration { get; set; }
    }
}
