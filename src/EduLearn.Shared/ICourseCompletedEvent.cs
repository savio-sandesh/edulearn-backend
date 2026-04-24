namespace EduLearn.Shared;

public interface ICourseCompletedEvent
{
    Guid EnrollmentId { get; }
    int StudentId { get; }
    int CourseId { get; }
    DateTime CompletedAt { get; }
}
