namespace EduLearn.Shared;

public record CourseCompletedEvent : ICourseCompletedEvent
{
    public Guid EnrollmentId { get; init; }
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public DateTime CompletedAt { get; init; }
}