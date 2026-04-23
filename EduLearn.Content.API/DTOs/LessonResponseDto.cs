namespace EduLearn.Content.API.DTOs;

public class LessonResponseDto
{
    public int LessonId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
