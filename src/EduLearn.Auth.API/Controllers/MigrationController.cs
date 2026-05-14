using EduLearn.Auth.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EduLearn.Auth.API.Controllers;

[ApiController]
[Route("api/admin/migrate")]
[Authorize(Roles = "ADMIN")]
public class MigrationController : ControllerBase
{
    private readonly AuthDbContext _db;

    public MigrationController(AuthDbContext db)
    {
        _db = db;
    }

    [HttpPost("normalize-avatars")]
    public async Task<IActionResult> NormalizeAvatars()
    {
        var users = await _db.Users.ToListAsync();
        var updated = 0;

        foreach (var u in users)
        {
            if (string.IsNullOrWhiteSpace(u.AvatarUrl)) continue;

            var original = u.AvatarUrl.Trim();
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
                    // take filename portion only
                    var filename = Path.GetFileName(path);
                    if (!string.IsNullOrWhiteSpace(filename))
                    {
                        u.AvatarUrl = filename;
                        updated++;
                    }
                }
            }
            catch
            {
                // ignore malformed URLs
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Avatar normalization complete.", updated });
    }
}
