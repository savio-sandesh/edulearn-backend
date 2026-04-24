using EduLearn.Content.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Content.API.Data;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(x => x.LessonId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.ContentType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ContentUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => x.CourseId);
            entity.HasIndex(x => new { x.CourseId, x.DisplayOrder });
            entity.HasIndex(x => new { x.CourseId, x.IsPreview });
        });
    }
}
