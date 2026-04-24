namespace EduLearn.Enrollment.API.DTOs;

public class UpdateProgressRequestDto
{
    public int? CompletedLessons { get; set; }
    public int? TotalLessons { get; set; }
}
