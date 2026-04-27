namespace EduLearn.Assessment.API.DTOs;

public class QuizAttemptResponseDto
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public int Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Answers { get; set; } = "{}";
}
