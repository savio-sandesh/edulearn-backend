namespace EduLearn.Assessment.API.Models;

using System.Text.Json.Serialization;

public class QuizAttempt
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public int Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    // JSON dictionary: QuestionId -> StudentSelectedAnswer
    public string Answers { get; set; } = "{}";

    [JsonIgnore]
    public Quiz? Quiz { get; set; }
}
