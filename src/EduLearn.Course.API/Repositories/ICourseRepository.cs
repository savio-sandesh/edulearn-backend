using EduLearn.Course.API.Models;
using CourseModel = EduLearn.Course.API.Models.Course;

namespace EduLearn.Course.API.Repositories
{
    public interface ICourseRepository
    {
        Task<CourseModel?> FindByCourseIdAsync(int courseId);
        Task<IReadOnlyList<CourseModel>> FindByInstructorIdAsync(int instructorId);
        Task<IReadOnlyList<CourseModel>> FindByCategoryAsync(string category);
        Task<IReadOnlyList<CourseModel>> FindByIsPublishedAsync(bool isPublished);
        Task<IReadOnlyList<CourseModel>> FindPendingDeleteAsync();
        Task<IReadOnlyList<CourseModel>> SearchCoursesAsync(string searchTerm);
        Task<IReadOnlyList<CourseModel>> FindTopRatedAsync(int count);
        Task<string?> ResolveCategoryNameAsync(string categoryInput);
        Task<IReadOnlyList<string>> GetAllCategoryNamesAsync();
        Task<int> CountByInstructorIdAsync(int instructorId);
        Task IncrementEnrollmentAsync(int courseId);
        Task AddReviewAsync(Review review);
        Task<IReadOnlyList<Review>> FindReviewsByCourseIdAsync(int courseId);

        Task AddAsync(CourseModel course);
        Task UpdateAsync(CourseModel course);
        Task SaveChangesAsync();
        Task<bool> DeleteByIdAsync(int courseId);
    }
}
