namespace EduLearn.Progress.API.Models;

public class LessonProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal ProgressPercent { get; set; }
}
