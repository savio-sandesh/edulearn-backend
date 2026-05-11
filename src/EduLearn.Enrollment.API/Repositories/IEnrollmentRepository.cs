using EduLearn.Enrollment.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace EduLearn.Enrollment.API.Repositories;

public interface IEnrollmentRepository
{
    Task AddAsync(Models.Enrollment enrollment);
    Task UpdateAsync(Models.Enrollment enrollment);
    Task<Models.Enrollment?> FindByEnrollmentIdAsync(int enrollmentId);
    Task<IReadOnlyList<Models.Enrollment>> FindByStudentIdAsync(int studentId);
    Task<IReadOnlyList<Models.Enrollment>> FindByCourseIdAsync(int courseId);
    Task<Models.Enrollment?> FindByStudentAndCourseAsync(int studentId, int courseId);
    Task<bool> IsEnrolledAsync(int studentId, int courseId);
    Task<IReadOnlyList<Models.Enrollment>> FindCompletedAsync(int studentId);
    Task<IReadOnlyList<Models.Enrollment>> FindInProgressAsync(int studentId);
    Task<int> CountByCourseIdAsync(int courseId);
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy();
}
