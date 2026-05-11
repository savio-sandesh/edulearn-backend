using EduLearn.Content.API.DTOs;
using EduLearn.Content.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLearn.Content.API.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonController : ControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly IBlobService _blobService;

    public LessonController(ILessonService lessonService, IBlobService blobService)
    {
        _lessonService = lessonService;
        _blobService = blobService;
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPost]
    public async Task<IActionResult> AddLesson([FromBody] LessonCreateDto lesson)
    {
        try
        {
            var created = await _lessonService.AddLessonAsync(lesson);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN,STUDENT")]
    [HttpGet("byId/{lessonId:int}")]
    public async Task<IActionResult> GetById(int lessonId)
    {
        var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found." });
        }

        return Ok(lesson);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN,STUDENT")]
    [HttpGet("byCourse/{courseId:int}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var lessons = await _lessonService.GetLessonsByCourseAsync(courseId);
        return Ok(lessons);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN,STUDENT")]
    [HttpGet("ordered/{courseId:int}")]
    public async Task<IActionResult> GetOrdered(int courseId)
    {
        var lessons = await _lessonService.GetOrderedLessonsAsync(courseId);
        return Ok(lessons);
    }

    [AllowAnonymous]
    [HttpGet("preview/{courseId:int}")]
    public async Task<IActionResult> GetPreview(int courseId)
    {
        var lessons = await _lessonService.GetPreviewLessonsAsync(courseId);
        return Ok(lessons);
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPut("update/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] LessonUpdateDto lesson)
    {
        try
        {
            var updated = await _lessonService.UpdateLessonAsync(lessonId, lesson);
            if (updated == null)
            {
                return NotFound(new { message = "Lesson not found." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPut("reorder/{courseId:int}")]
    public async Task<IActionResult> Reorder(int courseId, [FromBody] ReorderLessonsRequestDto request)
    {
        try
        {
            await _lessonService.ReorderLessonsAsync(courseId, request.OrderedLessonIds);
            return Ok(new { message = "Lessons reordered successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPut("publish/{lessonId:int}")]
    public async Task<IActionResult> PublishLesson(int lessonId)
    {
        var published = await _lessonService.PublishLessonAsync(lessonId);
        if (!published)
        {
            return NotFound(new { message = "Lesson not found." });
        }

        return Ok(new { message = "Lesson published." });
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("lesson/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        var deleted = await _lessonService.DeleteLessonAsync(lessonId);
        if (!deleted)
        {
            return NotFound(new { message = "Lesson not found." });
        }

        return Ok(new { message = "Lesson deleted." });
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("allForCourse/{courseId:int}")]
    public async Task<IActionResult> DeleteAllForCourse(int courseId)
    {
        await _lessonService.DeleteAllForCourseAsync(courseId);
        return Ok(new { message = "All lessons deleted for course." });
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN,STUDENT")]
    [HttpGet("count/{courseId:int}")]
    public async Task<IActionResult> GetCount(int courseId)
    {
        var count = await _lessonService.GetLessonCountAsync(courseId);
        return Ok(new { courseId, count });
    }

    [AllowAnonymous]
    [HttpGet("totalDuration/{courseId:int}")]
    public async Task<IActionResult> GetTotalDuration(int courseId)
    {
        var totalMinutes = await _lessonService.GetTotalDurationAsync(courseId);
        return Ok(new { courseId, totalMinutes });
    }

    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpPost("upload-video")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadVideo([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Video file is required." });
        }

        try
        {
            var fileName = $"{Guid.NewGuid()}-{file.FileName}";
            using var stream = file.OpenReadStream();
            
            var url = await _blobService.UploadBlobAsync(stream, fileName, file.ContentType);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Video upload failed.", detail = ex.Message });
        }
    }
}
