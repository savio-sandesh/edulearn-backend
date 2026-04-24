using EduLearn.Enrollment.API.Models;

namespace EduLearn.Enrollment.API.DTOs;

public class EnrollmentResponseDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public EnrollmentStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool CertificateIssued { get; set; }
    public string? PaymentId { get; set; }
}
