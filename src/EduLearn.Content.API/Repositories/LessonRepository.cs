using EduLearn.Content.API.Data;
using EduLearn.Content.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EduLearn.Content.API.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly ContentDbContext _dbContext;

    public LessonRepository(ContentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Lesson lesson)
    {
        await _dbContext.Lessons.AddAsync(lesson);
    }

    public async Task<Lesson?> FindByLessonIdAsync(int lessonId)
    {
        return await _dbContext.Lessons.FirstOrDefaultAsync(x => x.LessonId == lessonId);
    }

    public async Task<IReadOnlyList<Lesson>> FindByCourseIdAsync(int courseId)
    {
        return await _dbContext.Lessons
            .Where(x => x.CourseId == courseId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Lesson>> FindByCourseIdOrderByDisplayOrderAsync(int courseId)
    {
        return await _dbContext.Lessons
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Lesson>> FindByContentTypeAsync(string contentType)
    {
        return await _dbContext.Lessons
            .Where(x => x.ContentType == contentType)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Lesson>> FindPreviewLessonsAsync(int courseId)
    {
        return await _dbContext.Lessons
            .Where(x => x.CourseId == courseId && x.IsPreview && x.IsPublished)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    public async Task<int> CountByCourseIdAsync(int courseId)
    {
        return await _dbContext.Lessons.CountAsync(x => x.CourseId == courseId);
    }

    public async Task<int> SumDurationByCourseIdAsync(int courseId)
    {
        return await _dbContext.Lessons
            .Where(x => x.CourseId == courseId)
            .SumAsync(x => x.DurationMinutes);
    }

    public async Task<int> UpdateDisplayOrderAsync(int lessonId, int displayOrder)
    {
        return await _dbContext.Lessons
            .Where(x => x.LessonId == lessonId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DisplayOrder, displayOrder));
    }

    public Task UpdateAsync(Lesson lesson)
    {
        _dbContext.Lessons.Update(lesson);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteByLessonIdAsync(int lessonId)
    {
        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(x => x.LessonId == lessonId);
        if (lesson == null)
        {
            return false;
        }

        _dbContext.Lessons.Remove(lesson);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task DeleteByCourseIdAsync(int courseId)
    {
        await _dbContext.Lessons
            .Where(x => x.CourseId == courseId)
            .ExecuteDeleteAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _dbContext.Database.BeginTransactionAsync();
    }
}
