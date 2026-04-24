namespace EduLearn.Content.API.DTOs;

public class LessonUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }
}
