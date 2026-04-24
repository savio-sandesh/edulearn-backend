using EduLearn.Content.API.DTOs;
using EduLearn.Content.API.Models;
using EduLearn.Content.API.Repositories;

namespace EduLearn.Content.API.Services;

public class LessonService : ILessonService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.Ordinal)
    {
        "VIDEO",
        "ARTICLE",
        "PDF",
        "QUIZ_LINK"
    };

    private readonly ILessonRepository _lessonRepository;
    private readonly IBlobService _blobService;

    public LessonService(ILessonRepository lessonRepository, IBlobService blobService)
    {
        _lessonRepository = lessonRepository;
        _blobService = blobService;
    }

    public async Task<LessonResponseDto> AddLessonAsync(LessonCreateDto lesson)
    {
        ValidateLessonInput(lesson.Title, lesson.ContentType, lesson.ContentUrl, lesson.DurationMinutes);

        var existingCount = await _lessonRepository.CountByCourseIdAsync(lesson.CourseId);
        var entity = new Lesson
        {
            CourseId = lesson.CourseId,
            Title = lesson.Title.Trim(),
            Description = lesson.Description?.Trim() ?? string.Empty,
            ContentType = NormalizeContentType(lesson.ContentType),
            ContentUrl = lesson.ContentUrl.Trim(),
            DurationMinutes = lesson.DurationMinutes,
            DisplayOrder = existingCount + 1,
            IsPreview = lesson.IsPreview,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };

        await _lessonRepository.AddAsync(entity);
        await _lessonRepository.SaveChangesAsync();

        return await MapToDtoAsync(entity);
    }

    public async Task<LessonResponseDto?> GetLessonByIdAsync(int lessonId)
    {
        var lesson = await _lessonRepository.FindByLessonIdAsync(lessonId);
        return lesson == null ? null : await MapToDtoAsync(lesson);
    }

    public async Task<IReadOnlyList<LessonResponseDto>> GetLessonsByCourseAsync(int courseId)
    {
        var lessons = await _lessonRepository.FindByCourseIdAsync(courseId);
        return await MapManyToDtoAsync(lessons);
    }

    public async Task<IReadOnlyList<LessonResponseDto>> GetOrderedLessonsAsync(int courseId)
    {
        var lessons = await _lessonRepository.FindByCourseIdOrderByDisplayOrderAsync(courseId);
        return await MapManyToDtoAsync(lessons);
    }

    public async Task<IReadOnlyList<LessonResponseDto>> GetPreviewLessonsAsync(int courseId)
    {
        var lessons = await _lessonRepository.FindPreviewLessonsAsync(courseId);
        return await MapManyToDtoAsync(lessons);
    }

    public async Task<LessonResponseDto?> UpdateLessonAsync(int lessonId, LessonUpdateDto lesson)
    {
        ValidateLessonInput(lesson.Title, lesson.ContentType, lesson.ContentUrl, lesson.DurationMinutes);

        var existing = await _lessonRepository.FindByLessonIdAsync(lessonId);
        if (existing == null)
        {
            return null;
        }

        existing.Title = lesson.Title.Trim();
        existing.Description = lesson.Description?.Trim() ?? string.Empty;
        existing.ContentType = NormalizeContentType(lesson.ContentType);
        existing.ContentUrl = lesson.ContentUrl.Trim();
        existing.DurationMinutes = lesson.DurationMinutes;
        existing.IsPreview = lesson.IsPreview;

        await _lessonRepository.SaveChangesAsync();
        return await MapToDtoAsync(existing);
    }

    public async Task ReorderLessonsAsync(int courseId, IList<int> orderedLessonIds)
    {
        if (orderedLessonIds == null || orderedLessonIds.Count == 0)
        {
            throw new ArgumentException("Ordered lesson IDs are required.");
        }

        var existingLessons = await _lessonRepository.FindByCourseIdOrderByDisplayOrderAsync(courseId);
        if (existingLessons.Count != orderedLessonIds.Count)
        {
            throw new ArgumentException("Ordered lesson IDs must include all lessons for the course exactly once.");
        }

        var existingIds = existingLessons.Select(x => x.LessonId).ToHashSet();
        var orderedIdSet = orderedLessonIds.ToHashSet();
        if (!existingIds.SetEquals(orderedIdSet) || orderedIdSet.Count != orderedLessonIds.Count)
        {
            throw new ArgumentException("Ordered lesson IDs must include each lesson exactly once with no duplicates.");
        }

        await using var transaction = await _lessonRepository.BeginTransactionAsync();

        for (var index = 0; index < orderedLessonIds.Count; index++)
        {
            var lessonId = orderedLessonIds[index];
            var updatedRows = await _lessonRepository.UpdateDisplayOrderAsync(lessonId, index + 1);
            if (updatedRows == 0)
            {
                throw new InvalidOperationException($"Lesson {lessonId} was not found during reorder.");
            }
        }

        await transaction.CommitAsync();
    }

    public async Task<bool> PublishLessonAsync(int lessonId)
    {
        var lesson = await _lessonRepository.FindByLessonIdAsync(lessonId);
        if (lesson == null)
        {
            return false;
        }

        lesson.IsPublished = true;
        await _lessonRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsPremiumLessonAsync(int lessonId)
    {
        var lesson = await _lessonRepository.FindByLessonIdAsync(lessonId);
        if (lesson == null)
        {
            return false;
        }

        return !lesson.IsPreview;
    }

    public async Task<bool> CompleteLessonAsync(int lessonId)
    {
        var lesson = await _lessonRepository.FindByLessonIdAsync(lessonId);
        if (lesson == null)
        {
            return false;
        }

        lesson.IsCompleted = true;
        await _lessonRepository.UpdateAsync(lesson);
        await _lessonRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLessonAsync(int lessonId)
    {
        return await _lessonRepository.DeleteByLessonIdAsync(lessonId);
    }

    public async Task DeleteAllForCourseAsync(int courseId)
    {
        await _lessonRepository.DeleteByCourseIdAsync(courseId);
    }

    public async Task<int> GetLessonCountAsync(int courseId)
    {
        return await _lessonRepository.CountByCourseIdAsync(courseId);
    }

    private static string NormalizeContentType(string contentType)
    {
        var normalized = contentType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!AllowedContentTypes.Contains(normalized))
        {
            throw new ArgumentException("ContentType must be VIDEO, ARTICLE, PDF, or QUIZ_LINK.");
        }

        return normalized;
    }

    private static void ValidateLessonInput(string title, string contentType, string contentUrl, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType is required.");
        }

        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            throw new ArgumentException("ContentUrl is required.");
        }

        if (!Uri.TryCreate(contentUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("ContentUrl must be an absolute URL.");
        }

        if (durationMinutes < 0)
        {
            throw new ArgumentException("DurationMinutes must be zero or greater.");
        }
    }

    private async Task<IReadOnlyList<LessonResponseDto>> MapManyToDtoAsync(IEnumerable<Lesson> lessons)
    {
        var mapped = await Task.WhenAll(lessons.Select(MapToDtoAsync));
        return mapped;
    }

    private async Task<LessonResponseDto> MapToDtoAsync(Lesson lesson)
    {
        var resolvedContentUrl = await _blobService.GenerateReadSasUrlAsync(lesson.ContentUrl);

        return new LessonResponseDto
        {
            LessonId = lesson.LessonId,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Description = lesson.Description,
            ContentType = lesson.ContentType,
            ContentUrl = resolvedContentUrl,
            DurationMinutes = lesson.DurationMinutes,
            DisplayOrder = lesson.DisplayOrder,
            IsPreview = lesson.IsPreview,
            IsPublished = lesson.IsPublished,
            CreatedAt = lesson.CreatedAt
        };
    }
}
