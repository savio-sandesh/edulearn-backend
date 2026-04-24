using EduLearn.Course.API.Models;
using Microsoft.EntityFrameworkCore;
using CourseModel = EduLearn.Course.API.Models.Course;

namespace EduLearn.Course.API.Data
{
    public class CourseDbContext : DbContext
    {
        public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options)
        {
        }

        public DbSet<CourseModel> Courses { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CourseCategory> CourseCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseModel>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CourseModel>()
                .HasIndex(c => c.InstructorId);

            modelBuilder.Entity<CourseModel>()
                .HasIndex(c => c.Category);

            modelBuilder.Entity<CourseCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<CourseCategory>()
                .HasData(
                    new CourseCategory { CourseCategoryId = 1, Name = "Development" },
                    new CourseCategory { CourseCategoryId = 2, Name = "Design" },
                    new CourseCategory { CourseCategoryId = 3, Name = "Business" },
                    new CourseCategory { CourseCategoryId = 4, Name = "Marketing" },
                    new CourseCategory { CourseCategoryId = 5, Name = "IT & Software" },
                    new CourseCategory { CourseCategoryId = 6, Name = "Personal Development" }
                );

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.CourseId, r.StudentId })
                .IsUnique();
        }
    }
}
