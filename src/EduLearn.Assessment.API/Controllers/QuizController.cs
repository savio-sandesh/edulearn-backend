using EduLearn.Assessment.API.DTOs;
using EduLearn.Assessment.API.Models;
using EduLearn.Assessment.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Assessment.API.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] Quiz quiz)
    {
        try
        {
            var created = await _quizService.CreateQuiz(quiz);
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
        var quiz = await _quizService.GetQuizById(id);
        return quiz == null ? NotFound(new { message = "Quiz not found" }) : Ok(quiz);
    }

    [HttpGet("byCourse/{courseId:int}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var quizzes = await _quizService.GetQuizzesByCourse(courseId);
        return Ok(quizzes);
    }

    [HttpGet("byLesson/{lessonId:int}")]
    public async Task<IActionResult> GetByLesson(int lessonId)
    {
        var quiz = await _quizService.GetQuizByLesson(lessonId);
        return quiz == null ? NotFound(new { message = "Quiz not found" }) : Ok(quiz);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateQuiz(int id, [FromBody] Quiz quiz)
    {
        try
        {
            var updated = await _quizService.UpdateQuiz(id, quiz);
            return updated == null ? NotFound(new { message = "Quiz not found" }) : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var deleted = await _quizService.DeleteQuiz(id);
        return deleted ? Ok(new { message = "Quiz deleted." }) : NotFound(new { message = "Quiz not found" });
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPut("publish/{id:int}")]
    public async Task<IActionResult> PublishQuiz(int id)
    {
        var published = await _quizService.PublishQuiz(id);
        return published ? Ok(new { message = "Quiz published." }) : NotFound(new { message = "Quiz not found" });
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpPost("{quizId:int}/startAttempt")]
    public async Task<IActionResult> StartAttempt(int quizId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        try
        {
            var attempt = await _quizService.StartAttempt(studentId, quizId);
            return Ok(MapToResponseDto(attempt));
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> SubmitAttempt(int attemptId, [FromBody] SubmitAttemptRequestDto request)
    {
        try
        {
            var attempt = await _quizService.SubmitAttempt(attemptId, request.Answers);
            return attempt == null ? NotFound(new { message = "Attempt not found" }) : Ok(MapToResponseDto(attempt));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpGet("{quizId:int}/attempts")]
    public async Task<IActionResult> GetAttemptsByStudent(int quizId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var attempts = await _quizService.GetAttemptsByStudent(studentId, quizId);
        return Ok(attempts);
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpGet("{quizId:int}/bestAttempt")]
    public async Task<IActionResult> GetBestAttempt(int quizId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var attempt = await _quizService.GetBestAttempt(studentId, quizId);
        return attempt == null ? NotFound(new { message = "No attempts found." }) : Ok(attempt);
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpGet("{quizId:int}/attemptCount")]
    public async Task<IActionResult> GetAttemptCount(int quizId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var count = await _quizService.GetAttemptCount(studentId, quizId);
        return Ok(new { quizId, studentId, attemptCount = count });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId);
    }

    private static QuizAttemptResponseDto MapToResponseDto(QuizAttempt attempt)
    {
        return new QuizAttemptResponseDto
        {
            AttemptId = attempt.AttemptId,
            QuizId = attempt.QuizId,
            StudentId = attempt.StudentId,
            Score = attempt.Score,
            IsPassed = attempt.IsPassed,
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt,
            Answers = attempt.Answers
        };
    }
}
