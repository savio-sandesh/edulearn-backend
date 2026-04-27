namespace EduLearn.Progress.API.Models;

using System.ComponentModel.DataAnnotations.Schema;

public class Certificate
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string CertificateUrl { get; set; } = string.Empty;

    [Column("IssueDate")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public string VerificationCode { get; set; } = string.Empty;
}
