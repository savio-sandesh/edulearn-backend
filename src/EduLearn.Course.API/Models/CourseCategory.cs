using System.ComponentModel.DataAnnotations;

namespace EduLearn.Course.API.Models
{
    public class CourseCategory
    {
        public int CourseCategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
