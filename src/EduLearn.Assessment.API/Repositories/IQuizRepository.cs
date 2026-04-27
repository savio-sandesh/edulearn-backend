using EduLearn.Assessment.API.Models;

namespace EduLearn.Assessment.API.Repositories;

public interface IQuizRepository
{
    Task<Quiz?> FindByQuizId(int quizId);
    Task<IReadOnlyList<Quiz>> FindByCourseId(int courseId);
    Task<Quiz?> FindByLessonId(int lessonId);

    Task<IReadOnlyList<QuizAttempt>> FindAttemptsByStudentAndQuiz(int studentId, int quizId);
    Task<QuizAttempt?> FindAttemptById(int attemptId);
    Task<int> CountAttempts(int studentId, int quizId);
    Task<QuizAttempt?> FindBestAttempt(int studentId, int quizId);

    Task AddQuiz(Quiz quiz);
    Task DeleteQuiz(Quiz quiz);
    Task AddAttempt(QuizAttempt attempt);
    Task SaveChanges();
}
