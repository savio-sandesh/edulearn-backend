using System.ComponentModel.DataAnnotations;

namespace EduLearn.Review.API.Models;

public class Review
{
    public int ReviewId { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
