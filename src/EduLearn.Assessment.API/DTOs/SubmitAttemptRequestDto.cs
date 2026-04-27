namespace EduLearn.Assessment.API.DTOs;

public class SubmitAttemptRequestDto
{
    public Dictionary<int, string> Answers { get; set; } = new();
}
