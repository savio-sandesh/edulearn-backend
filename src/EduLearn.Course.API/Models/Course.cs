using System.ComponentModel.DataAnnotations;

namespace EduLearn.Course.API.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public int InstructorId { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(BEGINNER|INTERMEDIATE|ADVANCED)$", ErrorMessage = "Level must be BEGINNER, INTERMEDIATE, or ADVANCED.")]
        public string Level { get; set; } = "BEGINNER";

        [Required]
        public string Language { get; set; } = "English";

        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; }
        public bool IsApproved { get; set; }
        public bool IsDeleteRequested { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Stored in minutes.
        public int TotalDuration { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageRating { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
