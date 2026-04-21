using System.ComponentModel.DataAnnotations;

namespace EduLearn.Course.API.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Course Course { get; set; } = null!;
    }
}
