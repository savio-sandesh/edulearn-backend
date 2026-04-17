using EduLearn.Auth.API.Models;
using EduLearn.Auth.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Auth.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IBlobService _blobService;

        public UserController(IUserService userService, IBlobService blobService)
        {
            _userService = userService;
            _blobService = blobService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role // STUDENT/INSTRUCTOR/ADMIN
            };

            var createdUser = await _userService.RegisterAsync(user, dto.Password);
            return Ok(new { message = "User registered successfully", userId = createdUser.UserId });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _userService.LoginAsync(dto.Email, dto.Password);
            
            if (token == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new { token });
        }

        [Authorize] 
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] string fullName, IFormFile? avatar)
        {
            // Token se UserId nikalna (ClaimTypes.NameIdentifier humne UserService mein set kiya tha)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);
            string? avatarUrl = null;

            if (avatar != null && avatar.Length > 0)
            {
                using var stream = avatar.OpenReadStream();
                // Azure Blob Storage par upload
                avatarUrl = await _blobService.UploadFileAsync(stream, avatar.FileName, avatar.ContentType);
            }

            var result = await _userService.UpdateProfileAsync(userId, fullName, avatarUrl);

            if (!result) return NotFound(new { message = "User not found" });

            return Ok(new { message = "Profile updated successfully", avatarUrl });
        }
    }

    public record RegisterDto(string FullName, string Email, string Password, string Role);
    public record LoginDto(string Email, string Password);
}