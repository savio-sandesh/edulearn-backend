namespace EduLearn.Review.API.DTOs;

public class EnrollmentStatusDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public bool IsEnrolled { get; set; }
}
