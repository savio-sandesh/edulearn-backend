using EduLearn.Content.API.DTOs;

namespace EduLearn.Content.API.Services;

public interface ILessonService
{
    Task<LessonResponseDto> AddLessonAsync(LessonCreateDto lesson);
    Task<LessonResponseDto?> GetLessonByIdAsync(int lessonId);
    Task<IReadOnlyList<LessonResponseDto>> GetLessonsByCourseAsync(int courseId);
    Task<IReadOnlyList<LessonResponseDto>> GetOrderedLessonsAsync(int courseId);
    Task<IReadOnlyList<LessonResponseDto>> GetPreviewLessonsAsync(int courseId);
    Task<LessonResponseDto?> UpdateLessonAsync(int lessonId, LessonUpdateDto lesson);
    Task ReorderLessonsAsync(int courseId, IList<int> orderedLessonIds);
    Task<bool> PublishLessonAsync(int lessonId);
    Task<bool> IsPremiumLessonAsync(int lessonId);
    Task<bool> CompleteLessonAsync(int lessonId);
    Task<bool> DeleteLessonAsync(int lessonId);
    Task DeleteAllForCourseAsync(int courseId);
    Task<int> GetLessonCountAsync(int courseId);
    Task<int> GetTotalDurationAsync(int courseId);
}
