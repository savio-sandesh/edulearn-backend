using System.ComponentModel.DataAnnotations;

namespace EduLearn.Review.API.DTOs;

public class CreateReviewDto
{
    [Required]
    public int CourseId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
