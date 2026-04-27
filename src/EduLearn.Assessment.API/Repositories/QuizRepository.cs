using EduLearn.Assessment.API.Data;
using EduLearn.Assessment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Assessment.API.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly AssessmentDbContext _dbContext;

    public QuizRepository(AssessmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Quiz?> FindByQuizId(int quizId)
    {
        return await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
    }

    public async Task<IReadOnlyList<Quiz>> FindByCourseId(int courseId)
    {
        return await _dbContext.Quizzes
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.QuizId)
            .ToListAsync();
    }

    public async Task<Quiz?> FindByLessonId(int lessonId)
    {
        return await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.LessonId == lessonId);
    }

    public async Task<IReadOnlyList<QuizAttempt>> FindAttemptsByStudentAndQuiz(int studentId, int quizId)
    {
        return await _dbContext.QuizAttempts
            .Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
    }

    public async Task<QuizAttempt?> FindAttemptById(int attemptId)
    {
        return await _dbContext.QuizAttempts.FirstOrDefaultAsync(a => a.AttemptId == attemptId);
    }

    public async Task<int> CountAttempts(int studentId, int quizId)
    {
        return await _dbContext.QuizAttempts.CountAsync(a => a.StudentId == studentId && a.QuizId == quizId);
    }

    public async Task<QuizAttempt?> FindBestAttempt(int studentId, int quizId)
    {
        return await _dbContext.QuizAttempts
            .Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.Score)
            .ThenBy(a => a.AttemptId)
            .FirstOrDefaultAsync();
    }

    public async Task AddQuiz(Quiz quiz)
    {
        await _dbContext.Quizzes.AddAsync(quiz);
    }

    public Task DeleteQuiz(Quiz quiz)
    {
        _dbContext.Quizzes.Remove(quiz);
        return Task.CompletedTask;
    }

    public async Task AddAttempt(QuizAttempt attempt)
    {
        await _dbContext.QuizAttempts.AddAsync(attempt);
    }

    public async Task SaveChanges()
    {
        await _dbContext.SaveChangesAsync();
    }
}
