namespace EduLearn.Enrollment.API.Services;

public interface IProgressService
{
    Task<CourseProgressSnapshot> GetCourseProgressAsync(int studentId, int courseId);
}

public sealed class CourseProgressSnapshot
{
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public bool AllQuizzesPassed { get; set; }
}
