using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduLearn.Course.API.DTOs
{
    public class ThumbnailUploadRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
