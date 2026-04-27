using EduLearn.Progress.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Progress.API.Data;

public class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options)
        : base(options)
    {
    }

    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonProgress>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<LessonProgress>()
            .Property(x => x.ProgressPercent)
            .HasPrecision(5, 2);

        modelBuilder.Entity<LessonProgress>()
            .HasIndex(x => new { x.StudentId, x.CourseId, x.LessonId })
            .IsUnique();

        modelBuilder.Entity<Certificate>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Certificate>()
            .Property(x => x.CertificateUrl)
            .HasMaxLength(500);

        modelBuilder.Entity<Certificate>()
            .Property(x => x.VerificationCode)
            .HasMaxLength(64);

        modelBuilder.Entity<Certificate>()
            .HasIndex(x => x.VerificationCode)
            .IsUnique();

        modelBuilder.Entity<Certificate>()
            .HasIndex(x => new { x.StudentId, x.CourseId })
            .IsUnique();
    }
}
