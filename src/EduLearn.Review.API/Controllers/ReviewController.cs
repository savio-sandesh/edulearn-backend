using System.Security.Claims;
using EduLearn.Review.API.DTOs;
using EduLearn.Review.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLearn.Review.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] CreateReviewDto reviewDto)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        try
        {
            var created = await _reviewService.AddReviewAsync(reviewDto, studentId);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("course/{id:int}")]
    public async Task<IActionResult> GetApprovedByCourse(int id)
    {
        var reviews = await _reviewService.GetApprovedReviewsByCourseAsync(id);
        return Ok(reviews);
    }

    [AllowAnonymous]
    [HttpGet("course/{id:int}/average")]
    public async Task<IActionResult> GetAverageRating(int id)
    {
        var average = await _reviewService.GetAverageRatingAsync(id);
        return Ok(average);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var approved = await _reviewService.ApproveReviewAsync(id);
        return approved
            ? Ok(new { message = "Review approved." })
            : NotFound(new { message = "Review not found or already approved." });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out userId);
    }
}
