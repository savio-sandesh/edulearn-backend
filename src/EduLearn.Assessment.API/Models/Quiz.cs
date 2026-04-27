namespace EduLearn.Assessment.API.Models;

public class Quiz
{
    public int QuizId { get; set; }
    public int CourseId { get; set; }
    public int? LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int PassingScore { get; set; }
    public int MaxAttempts { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // JSON dictionary: QuestionId -> CorrectAnswer
    public string QuestionsJson { get; set; } = "{}";

    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
