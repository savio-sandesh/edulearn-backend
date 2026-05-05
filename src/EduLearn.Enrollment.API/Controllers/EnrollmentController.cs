using EduLearn.Enrollment.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Enrollment.API.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPost("enroll/{courseId:int}")]
    public async Task<IActionResult> Enroll(int courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        try
        {
            var enrollment = await _enrollmentService.EnrollAsync(studentId, courseId);
            return Ok(enrollment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "STUDENT,INSTRUCTOR,ADMIN")]
    [HttpGet("byId/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var enrollment = await _enrollmentService.GetEnrollmentByIdAsync(id);
        return enrollment == null ? NotFound(new { message = "Enrollment not found." }) : Ok(enrollment);
    }

    [Authorize(Roles = "STUDENT")]
    [HttpGet("byStudent/{studentId:int}")]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        if (!CanAccessStudentData(studentId))
        {
            return Forbid();
        }

        var enrollments = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
        return Ok(enrollments);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("byCourse/{courseId:int}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var enrollments = await _enrollmentService.GetEnrollmentsByCourseAsync(courseId);
        return Ok(enrollments);
    }

    [Authorize(Roles = "STUDENT")]
    [HttpGet("isEnrolled/{courseId:int}")]
    public async Task<IActionResult> IsEnrolled(int courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var enrolled = await _enrollmentService.IsEnrolledAsync(studentId, courseId);
        return Ok(new { studentId, courseId, isEnrolled = enrolled });
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPut("progress/{enrollmentId:int}")]
    public async Task<IActionResult> UpdateProgress(int enrollmentId)
    {
        var updated = await _enrollmentService.UpdateProgressAsync(enrollmentId);
        return updated == null ? NotFound(new { message = "Enrollment not found." }) : Ok(updated);
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPut("progress/byCourse/{courseId:int}")]
    public async Task<IActionResult> UpdateProgressByCourse(int courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var enrollments = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
        var enrollment = enrollments.FirstOrDefault(e => e.CourseId == courseId);
        if (enrollment == null)
        {
            return NotFound(new { message = "Enrollment not found." });
        }

        var updated = await _enrollmentService.UpdateProgressAsync(enrollment.EnrollmentId);
        return updated == null ? NotFound(new { message = "Enrollment not found." }) : Ok(updated);
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPost("complete/{courseId:int}")]
    public async Task<IActionResult> Complete(int courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var done = await _enrollmentService.CompleteEnrollmentAsync(studentId, courseId);
        return done ? Ok(new { message = "Enrollment marked as completed." }) : NotFound(new { message = "Enrollment not found." });
    }

    [Authorize(Roles = "STUDENT,ADMIN")]
    [HttpPut("issueCert/{enrollmentId:int}")]
    public async Task<IActionResult> IssueCertificate(int enrollmentId)
    {
        var enrollment = await _enrollmentService.GetEnrollmentByIdAsync(enrollmentId);
        if (enrollment == null)
        {
            return NotFound(new { message = "Enrollment not found." });
        }

        if (User.IsInRole("STUDENT") && !CanAccessStudentData(enrollment.StudentId))
        {
            return Forbid();
        }

        try
        {
            var issued = await _enrollmentService.IssueCertificateAsync(enrollmentId);
            return issued ? Ok(new { message = "Certificate issued." }) : NotFound(new { message = "Enrollment not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "STUDENT")]
    [HttpPost("drop/{courseId:int}")]
    public async Task<IActionResult> DropCourse(int courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            return Unauthorized(new { message = "User ID claim is missing or invalid." });
        }

        var dropped = await _enrollmentService.DropCourseAsync(studentId, courseId);
        return dropped ? Ok(new { message = "Enrollment dropped." }) : NotFound(new { message = "Enrollment not found." });
    }

    [Authorize(Roles = "STUDENT")]
    [HttpGet("completed/{studentId:int}")]
    public async Task<IActionResult> GetCompleted(int studentId)
    {
        if (!CanAccessStudentData(studentId))
        {
            return Forbid();
        }

        var enrollments = await _enrollmentService.GetCompletedCoursesAsync(studentId);
        return Ok(enrollments);
    }

    [Authorize(Roles = "STUDENT")]
    [HttpGet("inProgress/{studentId:int}")]
    public async Task<IActionResult> GetInProgress(int studentId)
    {
        if (!CanAccessStudentData(studentId))
        {
            return Forbid();
        }

        var enrollments = await _enrollmentService.GetInProgressCoursesAsync(studentId);
        return Ok(enrollments);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("count/{courseId:int}")]
    public async Task<IActionResult> GetCount(int courseId)
    {
        var count = await _enrollmentService.GetEnrollmentCountAsync(courseId);
        return Ok(new { courseId, count });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out userId);
    }

    private bool CanAccessStudentData(int requestedStudentId)
    {
        return TryGetCurrentUserId(out var currentUserId) && currentUserId == requestedStudentId;
    }
}
