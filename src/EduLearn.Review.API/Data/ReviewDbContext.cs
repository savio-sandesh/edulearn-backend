using EduLearn.Review.API.Models;
using Microsoft.EntityFrameworkCore;
using ReviewEntity = EduLearn.Review.API.Models.Review;

namespace EduLearn.Review.API.Data;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReviewEntity>()
            .HasKey(r => r.ReviewId);

        modelBuilder.Entity<ReviewEntity>()
            .Property(r => r.Comment)
            .HasMaxLength(1000);

        modelBuilder.Entity<ReviewEntity>()
            .Property(r => r.IsApproved)
            .HasDefaultValue(false);

        modelBuilder.Entity<ReviewEntity>()
            .HasIndex(r => new { r.CourseId, r.StudentId })
            .IsUnique();

        modelBuilder.Entity<ReviewEntity>()
            .ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "Rating >= 1 AND Rating <= 5"));
    }
}
