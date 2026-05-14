using EduLearn.Course.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EduLearn.Course.API.Controllers;

[ApiController]
[Route("api/admin/migrate")]
[Authorize(Roles = "ADMIN")]
public class MigrationController : ControllerBase
{
    private readonly CourseDbContext _db;

    public MigrationController(CourseDbContext db)
    {
        _db = db;
    }

    [HttpPost("normalize-thumbnails")]
    public async Task<IActionResult> NormalizeThumbnails()
    {
        var courses = await _db.Courses.ToListAsync();
        var updated = 0;

        foreach (var c in courses)
        {
            if (string.IsNullOrWhiteSpace(c.ThumbnailUrl)) continue;

            var original = c.ThumbnailUrl.Trim();
            if (!original.Contains("?sv=", StringComparison.OrdinalIgnoreCase)
                && !original.Contains("?sig=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                if (Uri.TryCreate(original, UriKind.Absolute, out var uri))
                {
                    var path = uri.LocalPath.Trim('/');
                    var filename = Path.GetFileName(path);
                    if (!string.IsNullOrWhiteSpace(filename))
                    {
                        c.ThumbnailUrl = filename;
                        updated++;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Thumbnail normalization complete.", updated });
    }
}
