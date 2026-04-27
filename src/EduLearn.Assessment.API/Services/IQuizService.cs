using EduLearn.Assessment.API.Models;

namespace EduLearn.Assessment.API.Services;

public interface IQuizService
{
    Task<Quiz> CreateQuiz(Quiz quiz);
    Task<Quiz?> GetQuizById(int id);
    Task<IReadOnlyList<Quiz>> GetQuizzesByCourse(int courseId);
    Task<Quiz?> GetQuizByLesson(int lessonId);
    Task<Quiz?> UpdateQuiz(int id, Quiz quiz);
    Task<bool> DeleteQuiz(int id);
    Task<bool> PublishQuiz(int id);

    Task<QuizAttempt> StartAttempt(int studentId, int quizId);
    Task<QuizAttempt?> SubmitAttempt(int attemptId, Dictionary<int, string> answers);
    Task<IReadOnlyList<QuizAttempt>> GetAttemptsByStudent(int studentId, int quizId);
    Task<QuizAttempt?> GetBestAttempt(int studentId, int quizId);
    Task<int> GetAttemptCount(int studentId, int quizId);
}
