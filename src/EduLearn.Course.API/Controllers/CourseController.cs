using EduLearn.Course.API.DTOs;
using EduLearn.Course.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Course.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IBlobService _blobService;

        public CourseController(ICourseService courseService, IBlobService blobService)
        {
            _courseService = courseService;
            _blobService = blobService;
        }

        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto course)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                var created = await _courseService.CreateCourseAsync(course, currentUserId);
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            return Ok(course);
        }

        [HttpGet("byInstructor/{instructorId:int}")]
        public async Task<IActionResult> GetByInstructor(int instructorId)
        {
            var courses = await _courseService.GetCoursesByInstructorAsync(instructorId);
            return Ok(courses);
        }

        [HttpGet("byCategory/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var courses = await _courseService.GetCoursesByCategoryAsync(category);
            return Ok(courses);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _courseService.GetAvailableCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("published")]
        public async Task<IActionResult> GetPublished()
        {
            var courses = await _courseService.GetPublishedCoursesAsync();
            return Ok(courses);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var courses = await _courseService.GetPendingApprovalCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Query parameter 'q' is required." });
            }

            var courses = await _courseService.SearchCoursesAsync(q);
            return Ok(courses);
        }

        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseUpdateDto course)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                var updated = await _courseService.UpdateCourseAsync(id, course, currentUserId, User.IsInRole("ADMIN"));
                if (updated == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpPut("publish/{id:int}")]
        public async Task<IActionResult> PublishCourse(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                var published = await _courseService.PublishCourseAsync(id, currentUserId, User.IsInRole("ADMIN"));
                if (!published)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(new { message = "Course published and pending admin approval." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpPost("{id:int}/thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadThumbnail(int id, [FromForm] ThumbnailUploadRequestDto request)
        {
            var file = request.File;

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Thumbnail image file is required." });
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only image files are allowed." });
            }

            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadFileAsync(stream, file.FileName, file.ContentType);

                var updated = await _courseService.UpdateThumbnailUrlAsync(id, imageUrl, currentUserId, User.IsInRole("ADMIN"));
                if (updated == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Thumbnail upload failed.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("reject/{id:int}")]
        public async Task<IActionResult> RejectCourse(int id)
        {
            var rejected = await _courseService.RejectCourseAsync(id);
            if (!rejected)
            {
                return NotFound(new { message = "Course not found" });
            }

            return Ok(new { message = "Course rejected and unpublished." });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("approve/{id:int}")]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var approved = await _courseService.ApproveCourseAsync(id);
            if (!approved)
            {
                return BadRequest(new { message = "Course not found or not published yet." });
            }

            return Ok(new { message = "Course approved successfully." });
        }

        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                var deleted = await _courseService.DeleteCourseAsync(id, currentUserId, User.IsInRole("ADMIN"));
                if (!deleted)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(new { message = "Course deleted successfully." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("topRated")]
        public async Task<IActionResult> GetTopRated([FromQuery] int count = 10)
        {
            if (count <= 0)
            {
                return BadRequest(new { message = "Count must be greater than zero." });
            }

            var courses = await _courseService.GetTopRatedCoursesAsync(count);
            return Ok(courses);
        }

        [Authorize]
        [HttpPost("{id:int}/enrollments/increment")]
        public async Task<IActionResult> IncrementEnrollment(int id)
        {
            var updated = await _courseService.IncrementEnrollmentAsync(id);
            if (!updated)
            {
                return NotFound(new { message = "Course not found" });
            }

            return Ok(new { message = "Enrollment count incremented." });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId);
        }
    }
}
