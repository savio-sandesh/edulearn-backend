using EduLearn.Assessment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Assessment.API.Data;

public class AssessmentDbContext : DbContext
{
    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quiz>()
            .HasKey(q => q.QuizId);

        modelBuilder.Entity<Quiz>()
            .Property(q => q.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<Quiz>()
            .Property(q => q.Description)
            .HasMaxLength(4000);

        modelBuilder.Entity<Quiz>()
            .ToTable(t =>
            {
                t.HasCheckConstraint("CK_Quiz_PassingScore", "PassingScore >= 0 AND PassingScore <= 100");
                t.HasCheckConstraint("CK_Quiz_MaxAttempts", "MaxAttempts > 0");
            });

        modelBuilder.Entity<Quiz>()
            .HasIndex(q => q.CourseId);

        modelBuilder.Entity<Quiz>()
            .HasIndex(q => q.LessonId);

        modelBuilder.Entity<QuizAttempt>()
            .HasKey(a => a.AttemptId);

        modelBuilder.Entity<QuizAttempt>()
            .HasOne(a => a.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizAttempt>()
            .HasIndex(a => new { a.StudentId, a.QuizId });

        modelBuilder.Entity<QuizAttempt>()
            .ToTable(t => t.HasCheckConstraint("CK_QuizAttempt_Score", "Score >= 0 AND Score <= 100"));
    }
}
