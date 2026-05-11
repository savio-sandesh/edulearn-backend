using EduLearn.Enrollment.API.Data;
using EduLearn.Enrollment.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EduLearn.Enrollment.API.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly EnrollmentDbContext _dbContext;

    public EnrollmentRepository(EnrollmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Models.Enrollment enrollment)
    {
        await _dbContext.Enrollments.AddAsync(enrollment);
    }

    public Task UpdateAsync(Models.Enrollment enrollment)
    {
        _dbContext.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }

    public async Task<Models.Enrollment?> FindByEnrollmentIdAsync(int enrollmentId)
    {
        return await _dbContext.Enrollments.FirstOrDefaultAsync(x => x.EnrollmentId == enrollmentId);
    }

    public async Task<IReadOnlyList<Models.Enrollment>> FindByStudentIdAsync(int studentId)
    {
        return await _dbContext.Enrollments
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.EnrolledAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.Enrollment>> FindByCourseIdAsync(int courseId)
    {
        return await _dbContext.Enrollments
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.EnrolledAt)
            .ToListAsync();
    }

    public async Task<Models.Enrollment?> FindByStudentAndCourseAsync(int studentId, int courseId)
    {
        return await _dbContext.Enrollments
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId);
    }

    public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
    {
        return await _dbContext.Enrollments.AnyAsync(x =>
            x.StudentId == studentId &&
            x.CourseId == courseId &&
            x.Status != EnrollmentStatus.DROPPED);
    }

    public async Task<IReadOnlyList<Models.Enrollment>> FindCompletedAsync(int studentId)
    {
        return await _dbContext.Enrollments
            .Where(x => x.StudentId == studentId && x.Status == EnrollmentStatus.COMPLETED)
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.Enrollment>> FindInProgressAsync(int studentId)
    {
        return await _dbContext.Enrollments
            .Where(x => x.StudentId == studentId && x.Status == EnrollmentStatus.ACTIVE)
            .OrderByDescending(x => x.LastAccessedAt)
            .ToListAsync();
    }

    public async Task<int> CountByCourseIdAsync(int courseId)
    {
        return await _dbContext.Enrollments.CountAsync(x => x.CourseId == courseId && x.Status != EnrollmentStatus.DROPPED);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _dbContext.Database.BeginTransactionAsync();
    }

    public Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy()
    {
        return _dbContext.Database.CreateExecutionStrategy();
    }
}
