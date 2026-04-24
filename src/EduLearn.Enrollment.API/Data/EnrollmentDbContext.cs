using EduLearn.Enrollment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Enrollment.API.Data;

public class EnrollmentDbContext : DbContext
{
    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Enrollment> Enrollments => Set<Models.Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Enrollment>(entity =>
        {
            entity.HasKey(x => x.EnrollmentId);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ProgressPercent).HasDefaultValue(0);
            entity.Property(x => x.PaymentId).HasMaxLength(120);

            entity.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            entity.HasIndex(x => x.CourseId);
            entity.HasIndex(x => x.StudentId);
        });
    }
}
