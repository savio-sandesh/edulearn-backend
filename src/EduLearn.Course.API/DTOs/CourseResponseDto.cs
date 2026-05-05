namespace EduLearn.Course.API.DTOs
{
    public class CourseResponseDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int InstructorId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPublished { get; set; }
        public bool IsApproved { get; set; }
        public bool IsDeleteRequested { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalDuration { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageRating { get; set; }
    }
}
