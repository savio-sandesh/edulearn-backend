using EduLearn.Course.API.DTOs;
using EduLearn.Course.API.Models;
using EduLearn.Course.API.Repositories;
using Microsoft.EntityFrameworkCore;
using CourseModel = EduLearn.Course.API.Models.Course;

namespace EduLearn.Course.API.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IReviewServiceClient _reviewServiceClient;
        private readonly EduLearn.Shared.Services.ISharedBlobService _blobService;

        public CourseService(ICourseRepository courseRepository, IReviewServiceClient reviewServiceClient, EduLearn.Shared.Services.ISharedBlobService blobService)
        {
            _courseRepository = courseRepository;
            _reviewServiceClient = reviewServiceClient;
            _blobService = blobService;
        }

        public async Task<CourseResponseDto> CreateCourseAsync(CourseCreateDto courseDto, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(courseDto.Title))
            {
                throw new ArgumentException("Title is required.");
            }

            var course = MapToEntity(courseDto);
            course.Level = NormalizeLevel(course.Level);
            course.Category = await NormalizeAndValidateCategoryAsync(course.Category);
            course.Language = course.Language.Trim();
            course.InstructorId = currentUserId;
            course.CreatedAt = DateTime.UtcNow;
            course.UpdatedAt = DateTime.UtcNow;
            course.IsPublished = false;
            course.IsApproved = false;
            course.EnrollmentCount = 0;

            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();
            
            var dto = MapToResponseDto(course);
            await EnrichCourseWithSasUrlAsync(dto);
            return dto;
        }

        public async Task<CourseResponseDto?> GetCourseByIdAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return null;
            }

            var dto = MapToResponseDto(course);
            
            // Enrich with SAS URL if thumbnail is a blob filename
            await EnrichCourseWithSasUrlAsync(dto);
            
            // Fetch live average rating from Review API
            dto.AverageRating = await _reviewServiceClient.GetAverageRatingAsync(courseId);
            return dto;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetCoursesByInstructorAsync(int instructorId)
        {
            var courses = await _courseRepository.FindByInstructorIdAsync(instructorId);
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            return dtos;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetCoursesByCategoryAsync(string category)
        {
            var courses = await _courseRepository.FindByCategoryAsync(category);
            
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            // Fetch live average ratings from Review API for all courses
            foreach (var dto in dtos)
            {
                dto.AverageRating = await _reviewServiceClient.GetAverageRatingAsync(dto.CourseId);
            }
            
            return dtos;
        }

        public async Task<IReadOnlyList<string>> GetAvailableCategoriesAsync()
        {
            return await _courseRepository.GetAllCategoryNamesAsync();
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetPublishedCoursesAsync()
        {
            var published = await _courseRepository.FindByIsPublishedAsync(true);
            var courses = published.Where(c => c.IsApproved).ToList();
            
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            // Fetch live average ratings from Review API for all courses
            foreach (var dto in dtos)
            {
                dto.AverageRating = await _reviewServiceClient.GetAverageRatingAsync(dto.CourseId);
            }
            
            return dtos;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetPendingApprovalCoursesAsync()
        {
            var published = await _courseRepository.FindByIsPublishedAsync(true);
            var pending = published.Where(c => !c.IsApproved).ToList();
            var dtos = pending.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            return dtos;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetPendingDeleteCoursesAsync()
        {
            var courses = await _courseRepository.FindPendingDeleteAsync();
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            return dtos;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> SearchCoursesAsync(string searchTerm)
        {
            var results = await _courseRepository.SearchCoursesAsync(searchTerm);
            var courses = results.Where(c => c.IsPublished && c.IsApproved).ToList();
            
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            // Fetch live average ratings from Review API for all courses
            foreach (var dto in dtos)
            {
                dto.AverageRating = await _reviewServiceClient.GetAverageRatingAsync(dto.CourseId);
            }
            
            return dtos;
        }

        public async Task<CourseResponseDto?> UpdateCourseAsync(int courseId, CourseUpdateDto updatedCourse, int currentUserId, bool isAdmin)
        {
            var existing = await _courseRepository.FindByCourseIdAsync(courseId);
            if (existing == null)
            {
                return null;
            }

            EnsureCanModifyCourse(existing, currentUserId, isAdmin);

            MapUpdateDtoOntoEntity(updatedCourse, existing);
            existing.Category = await NormalizeAndValidateCategoryAsync(existing.Category);
            existing.Level = NormalizeLevel(existing.Level);
            existing.Language = existing.Language.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            // Updated content must go through publish/approve workflow again.
            existing.IsPublished = false;
            existing.IsApproved = false;

            await _courseRepository.SaveChangesAsync();
            
            var dto = MapToResponseDto(existing);
            await EnrichCourseWithSasUrlAsync(dto);
            return dto;
        }

        public async Task<CourseResponseDto?> UpdateThumbnailUrlAsync(int courseId, string thumbnailUrl, int currentUserId, bool isAdmin)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return null;
            }

            EnsureCanModifyCourse(course, currentUserId, isAdmin);

            course.ThumbnailUrl = NormalizeThumbnailReference(thumbnailUrl);
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepository.SaveChangesAsync();
            
            var dto = MapToResponseDto(course);
            await EnrichCourseWithSasUrlAsync(dto);
            return dto;
        }

        public async Task<bool> PublishCourseAsync(int courseId, int currentUserId, bool isAdmin)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            EnsureCanModifyCourse(course, currentUserId, isAdmin);

            course.IsPublished = true;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveCourseAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            // Enforce two-step workflow: only published courses can be approved.
            if (!course.IsPublished)
            {
                return false;
            }

            course.IsApproved = true;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectCourseAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            // Rejection makes the course non-public until instructor edits and republishes.
            course.IsApproved = false;
            course.IsPublished = false;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int courseId, int currentUserId, bool isAdmin)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            EnsureCanModifyCourse(course, currentUserId, isAdmin);

            if (!isAdmin)
            {
                // Soft delete: pending admin approval
                course.IsDeleteRequested = true;
                course.IsPublished = false; 
                course.UpdatedAt = DateTime.UtcNow;
                await _courseRepository.SaveChangesAsync();
                return true;
            }

            // Admin deletion is permanent
            return await _courseRepository.DeleteByIdAsync(courseId);
        }

        public async Task<bool> RejectDeleteAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            if (!course.IsDeleteRequested)
            {
                return false;
            }

            course.IsDeleteRequested = false;
            // Optionally, we could set IsPublished back to true, but safer to let instructor republish.
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> GetTopRatedCoursesAsync(int count)
        {
            var courses = await _courseRepository.FindTopRatedAsync(count);
            var dtos = courses.Select(MapToResponseDto).ToList();
            
            // Enrich thumbnails with SAS URLs
            await EnrichCoursesWithSasUrlsAsync(dtos);
            
            return dtos;
        }

        public async Task<bool> IncrementEnrollmentAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return false;
            }

            course.EnrollmentCount += 1;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();
            return true;
        }

        public async Task<ReviewResponseDto?> AddReviewAsync(ReviewCreateDto reviewDto, int currentUserId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(reviewDto.CourseId);
            if (course == null)
            {
                return null;
            }

            if (!course.IsPublished || !course.IsApproved)
            {
                throw new ArgumentException("You can only review published and approved courses.");
            }

            var review = new Review
            {
                CourseId = reviewDto.CourseId,
                StudentId = currentUserId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _courseRepository.AddReviewAsync(review);
                await _courseRepository.SaveChangesAsync();

                // Recalculate average rating
                var allReviews = await _courseRepository.FindReviewsByCourseIdAsync(course.CourseId);
                double newAvg = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
                course.AverageRating = newAvg;
                await _courseRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ArgumentException("You have already submitted a review for this course.");
            }

            return new ReviewResponseDto
            {
                ReviewId = review.ReviewId,
                CourseId = review.CourseId,
                StudentId = review.StudentId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }

        public async Task<IReadOnlyList<ReviewResponseDto>> GetReviewsAsync(int courseId)
        {
            var reviews = await _courseRepository.FindReviewsByCourseIdAsync(courseId);
            return reviews.Select(r => new ReviewResponseDto
            {
                ReviewId = r.ReviewId,
                CourseId = r.CourseId,
                StudentId = r.StudentId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        private static string NormalizeLevel(string level)
        {
            var normalized = level?.Trim().ToUpperInvariant() ?? string.Empty;
            return normalized switch
            {
                "BEGINNER" => normalized,
                "INTERMEDIATE" => normalized,
                "ADVANCED" => normalized,
                _ => throw new ArgumentException("Level must be BEGINNER, INTERMEDIATE, or ADVANCED.")
            };
        }

        private async Task<string> NormalizeAndValidateCategoryAsync(string category)
        {
            var trimmed = category?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new ArgumentException("Category is required.");
            }

            var resolved = await _courseRepository.ResolveCategoryNameAsync(trimmed);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }

            var allowed = await _courseRepository.GetAllCategoryNamesAsync();
            throw new ArgumentException($"Invalid category. Allowed categories: {string.Join(", ", allowed)}");
        }

        private static void EnsureCanModifyCourse(CourseModel course, int currentUserId, bool isAdmin)
        {
            if (!isAdmin && course.InstructorId != currentUserId)
            {
                throw new UnauthorizedAccessException("You can only modify your own courses.");
            }
        }

        private static CourseModel MapToEntity(CourseCreateDto dto)
        {
            return new CourseModel
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Level = dto.Level,
                Language = dto.Language,
                Price = dto.Price,
                ThumbnailUrl = NormalizeThumbnailReference(dto.ThumbnailUrl),
                TotalDuration = dto.TotalDuration
            };
        }

        private static void MapUpdateDtoOntoEntity(CourseUpdateDto dto, CourseModel course)
        {
            course.Title = dto.Title;
            course.Description = dto.Description;
            course.Category = dto.Category;
            course.Level = dto.Level;
            course.Language = dto.Language;
            course.Price = dto.Price;
            course.TotalDuration = dto.TotalDuration;

            // Only overwrite the thumbnail when the caller explicitly provides a new URL.
            // If the DTO omits ThumbnailUrl (null/empty), keep the existing stored value.
            if (!string.IsNullOrWhiteSpace(dto.ThumbnailUrl))
            {
                course.ThumbnailUrl = NormalizeThumbnailReference(dto.ThumbnailUrl);
            }

            course.ThumbnailUrl = NormalizeThumbnailReference(course.ThumbnailUrl);
        }

        private static CourseResponseDto MapToResponseDto(CourseModel course)
        {
            return new CourseResponseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                Category = course.Category,
                Level = course.Level,
                Language = course.Language,
                Price = course.Price,
                ThumbnailUrl = course.ThumbnailUrl,
                IsPublished = course.IsPublished,
                IsApproved = course.IsApproved,
                IsDeleteRequested = course.IsDeleteRequested,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                TotalDuration = course.TotalDuration,
                EnrollmentCount = course.EnrollmentCount,
                AverageRating = course.AverageRating
            };
        }

        /// <summary>
        /// Enriches a course DTO with a signed SAS URL for the thumbnail.
        /// If ThumbnailUrl is a filename (not starting with "http"), generates a read-only SAS URL.
        /// </summary>
        private async Task EnrichCourseWithSasUrlAsync(CourseResponseDto dto)
        {
            var thumbnailReference = TryGetBlobReference(dto.ThumbnailUrl);
            if (!string.IsNullOrEmpty(thumbnailReference))
            {
                try
                {
                    dto.ThumbnailUrl = await _blobService.GenerateReadSasUrlAsync(thumbnailReference, "course-thumbnails");
                }
                catch
                {
                    // If SAS generation fails, leave the stored reference as-is.
                }
            }
        }

        /// <summary>
        /// Enriches a collection of courses with signed SAS URLs.
        /// </summary>
        private async Task EnrichCoursesWithSasUrlsAsync(IReadOnlyList<CourseResponseDto> courses)
        {
            foreach (var course in courses)
            {
                await EnrichCourseWithSasUrlAsync(course);
            }
        }

        private static string? NormalizeThumbnailReference(string? thumbnailUrl)
        {
            var blobReference = TryGetBlobReference(thumbnailUrl);
            if (blobReference != null)
            {
                return blobReference;
            }

            if (string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                return null;
            }

            return thumbnailUrl.Trim();
        }

        private static string? TryGetBlobReference(string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                return null;
            }

            var trimmed = thumbnailUrl.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                var relativePath = trimmed.Split('?', '#')[0].Replace('\\', '/');
                return string.IsNullOrWhiteSpace(relativePath) ? null : relativePath;
            }

            if (!uri.Host.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var firstSlash = path.IndexOf('/');
            var blobPath = firstSlash >= 0 ? path[(firstSlash + 1)..] : path;
            return string.IsNullOrWhiteSpace(blobPath) ? null : blobPath;
        }
    }
}
