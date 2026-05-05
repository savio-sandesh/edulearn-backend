using EduLearn.Course.API.Data;
using EduLearn.Course.API.Models;
using Microsoft.EntityFrameworkCore;
using CourseModel = EduLearn.Course.API.Models.Course;

namespace EduLearn.Course.API.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly CourseDbContext _context;

        public CourseRepository(CourseDbContext context)
        {
            _context = context;
        }

        public async Task<CourseModel?> FindByCourseIdAsync(int courseId)
        {
            return await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
        }

        public async Task<IReadOnlyList<CourseModel>> FindByInstructorIdAsync(int instructorId)
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CourseModel>> FindByCategoryAsync(string category)
        {
            var normalizedCategory = category.Trim().ToUpperInvariant();
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.Category.ToUpper() == normalizedCategory)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CourseModel>> FindByIsPublishedAsync(bool isPublished)
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.IsPublished == isPublished)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CourseModel>> FindPendingDeleteAsync()
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.IsDeleteRequested)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CourseModel>> SearchCoursesAsync(string searchTerm)
        {
            var pattern = $"%{searchTerm.Trim()}%";
            return await _context.Courses
                .AsNoTracking()
                .Where(c =>
                    EF.Functions.Like(c.Title, pattern) ||
                    EF.Functions.Like(c.Description, pattern) ||
                    EF.Functions.Like(c.Category, pattern))
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CourseModel>> FindTopRatedAsync(int count)
        {
            var topCourseIds = await _context.Reviews
                .GroupBy(r => r.CourseId)
                .Select(group => new
                {
                    CourseId = group.Key,
                    AvgRating = group.Average(r => r.Rating)
                })
                .OrderByDescending(x => x.AvgRating)
                .Take(count)
                .Select(x => x.CourseId)
                .ToListAsync();

            return await _context.Courses
                .AsNoTracking()
                .Where(c => topCourseIds.Contains(c.CourseId) && c.IsPublished && c.IsApproved)
                .ToListAsync();
        }

            public async Task<string?> ResolveCategoryNameAsync(string categoryInput)
            {
                var normalized = categoryInput.Trim().ToUpperInvariant();

                return await _context.CourseCategories
                .AsNoTracking()
                .Where(c => c.Name.ToUpper() == normalized)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            }

            public async Task<IReadOnlyList<string>> GetAllCategoryNamesAsync()
            {
                return await _context.CourseCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            }

        public async Task<int> CountByInstructorIdAsync(int instructorId)
        {
            return await _context.Courses.CountAsync(c => c.InstructorId == instructorId);
        }

        public async Task IncrementEnrollmentAsync(int courseId)
        {
            await _context.Courses
                .Where(c => c.CourseId == courseId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.EnrollmentCount, c => c.EnrollmentCount + 1)
                    .SetProperty(c => c.UpdatedAt, _ => DateTime.UtcNow));
        }

        public async Task AddReviewAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public async Task<IReadOnlyList<Review>> FindReviewsByCourseIdAsync(int courseId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Where(r => r.CourseId == courseId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(CourseModel course)
        {
            await _context.Courses.AddAsync(course);
        }

        public Task UpdateAsync(CourseModel course)
        {
            _context.Courses.Update(course);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteByIdAsync(int courseId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
            {
                return false;
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
