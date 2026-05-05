using EduLearn.Content.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace EduLearn.Content.API.Repositories;

public interface ILessonRepository
{
    Task AddAsync(Lesson lesson);
    Task<Lesson?> FindByLessonIdAsync(int lessonId);
    Task<IReadOnlyList<Lesson>> FindByCourseIdAsync(int courseId);
    Task<IReadOnlyList<Lesson>> FindByCourseIdOrderByDisplayOrderAsync(int courseId);
    Task<IReadOnlyList<Lesson>> FindByContentTypeAsync(string contentType);
    Task<IReadOnlyList<Lesson>> FindPreviewLessonsAsync(int courseId);
    Task<int> CountByCourseIdAsync(int courseId);
    Task<int> SumDurationByCourseIdAsync(int courseId);
    Task<int> UpdateDisplayOrderAsync(int lessonId, int displayOrder);
    Task UpdateAsync(Lesson lesson);
    Task SaveChangesAsync();
    Task<bool> DeleteByLessonIdAsync(int lessonId);
    Task DeleteByCourseIdAsync(int courseId);
    Task<IDbContextTransaction> BeginTransactionAsync();
}
