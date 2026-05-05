using EduLearn.Course.API.DTOs;

namespace EduLearn.Course.API.Services
{
    public interface ICourseService
    {
        Task<CourseResponseDto> CreateCourseAsync(CourseCreateDto course, int currentUserId);
        Task<CourseResponseDto?> GetCourseByIdAsync(int courseId);
        Task<IReadOnlyList<CourseResponseDto>> GetCoursesByInstructorAsync(int instructorId);
        Task<IReadOnlyList<CourseResponseDto>> GetCoursesByCategoryAsync(string category);
        Task<IReadOnlyList<string>> GetAvailableCategoriesAsync();
        Task<IReadOnlyList<CourseResponseDto>> GetPublishedCoursesAsync();
        Task<IReadOnlyList<CourseResponseDto>> GetPendingApprovalCoursesAsync();
        Task<IReadOnlyList<CourseResponseDto>> GetPendingDeleteCoursesAsync();
        Task<IReadOnlyList<CourseResponseDto>> SearchCoursesAsync(string searchTerm);
        Task<CourseResponseDto?> UpdateCourseAsync(int courseId, CourseUpdateDto updatedCourse, int currentUserId, bool isAdmin);
        Task<CourseResponseDto?> UpdateThumbnailUrlAsync(int courseId, string thumbnailUrl, int currentUserId, bool isAdmin);
        Task<bool> PublishCourseAsync(int courseId, int currentUserId, bool isAdmin);
        Task<bool> ApproveCourseAsync(int courseId);
        Task<bool> RejectCourseAsync(int courseId);
        Task<bool> DeleteCourseAsync(int courseId, int currentUserId, bool isAdmin);
        Task<bool> RejectDeleteAsync(int courseId);
        Task<IReadOnlyList<CourseResponseDto>> GetTopRatedCoursesAsync(int count);
        Task<bool> IncrementEnrollmentAsync(int courseId);
        Task<ReviewResponseDto?> AddReviewAsync(ReviewCreateDto reviewDto, int currentUserId);
        Task<IReadOnlyList<ReviewResponseDto>> GetReviewsAsync(int courseId);
    }
}
