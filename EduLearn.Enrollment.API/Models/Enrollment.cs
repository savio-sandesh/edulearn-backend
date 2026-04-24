namespace EduLearn.Enrollment.API.Models;

public enum EnrollmentStatus
{
    ACTIVE = 1,
    COMPLETED = 2,
    DROPPED = 3
}

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ACTIVE;
    public int ProgressPercent { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool CertificateIssued { get; set; }
    public string? PaymentId { get; set; }
}
