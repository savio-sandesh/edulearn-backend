using EduLearn.Content.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EduLearn.Content.API.Controllers;

[ApiController]
[Route("api/admin/migrate")]
[Authorize(Roles = "ADMIN")]
public class MigrationController : ControllerBase
{
    private readonly ContentDbContext _db;

    public MigrationController(ContentDbContext db)
    {
        _db = db;
    }

    [HttpPost("normalize-lessons")]
    public async Task<IActionResult> NormalizeLessons()
    {
        var lessons = await _db.Lessons.ToListAsync();
        var updated = 0;

        foreach (var l in lessons)
        {
            if (!string.IsNullOrWhiteSpace(l.ContentUrl) &&
                (l.ContentUrl.Contains("?sv=", StringComparison.OrdinalIgnoreCase) || l.ContentUrl.Contains("?sig=", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    if (Uri.TryCreate(l.ContentUrl, UriKind.Absolute, out var uri))
                    {
                        var path = uri.LocalPath.Trim('/');
                        var filename = Path.GetFileName(path);
                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            l.ContentUrl = filename;
                            updated++;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Lesson content normalization complete.", updated });
    }
}
