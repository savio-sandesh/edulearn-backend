using EduLearn.Course.API.DTOs;
using EduLearn.Course.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Course.API.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public ReviewController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [Authorize(Roles = "STUDENT")]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto review)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { message = "User ID claim is missing or invalid." });
            }

            try
            {
                var created = await _courseService.AddReviewAsync(review, currentUserId);
                if (created == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId);
        }
    }
}
