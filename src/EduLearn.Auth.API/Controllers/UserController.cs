using EduLearn.Auth.API.Models;
using EduLearn.Auth.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.Auth.API.Controllers
{
    /// <summary>
    /// Handles user registration, authentication, and profile endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IBlobService _blobService;

        /// <summary>
        /// Initializes a new instance of the user controller.
        /// </summary>
        /// <param name="userService">User business service.</param>
        /// <param name="blobService">Blob storage service.</param>
        public UserController(IUserService userService, IBlobService blobService)
        {
            _userService = userService;
            _blobService = blobService;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="dto">Registration payload.</param>
        /// <returns>Created user identifier when successful.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="dto">Login payload.</param>
        /// <returns>JWT token when credentials are valid.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var tokenPair = await _userService.LoginAsync(dto.Email, dto.Password);
            
            if (tokenPair == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new
            {
                accessToken = tokenPair.AccessToken,
                refreshToken = tokenPair.RefreshToken,
                accessTokenExpiresAt = tokenPair.AccessTokenExpiresAt
            });
        }

        /// <summary>
        /// Exchanges a refresh token for a new access token and refresh token pair.
        /// </summary>
        /// <param name="dto">Refresh-token payload.</param>
        /// <returns>New token pair when refresh is valid.</returns>
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required." });
            }

            var tokenPair = await _userService.RefreshTokenAsync(dto.RefreshToken);
            if (tokenPair == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            return Ok(new
            {
                accessToken = tokenPair.AccessToken,
                refreshToken = tokenPair.RefreshToken,
                accessTokenExpiresAt = tokenPair.AccessTokenExpiresAt
            });
        }

        /// <summary>
        /// Ends the current authenticated session from the API perspective.
        /// </summary>
        /// <returns>Operation status.</returns>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest(new { message = "Invalid user identifier in token." });
            }

            var loggedOut = await _userService.LogoutAsync(userId.Value);
            if (!loggedOut)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Validates whether a JWT token is currently valid.
        /// </summary>
        /// <param name="dto">Token payload.</param>
        /// <returns>Token validation status.</returns>
        [AllowAnonymous]
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] ValidateTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            var isValid = _userService.ValidateToken(dto.Token);
            return Ok(new { isValid });
        }

        /// <summary>
        /// Gets the authenticated user's profile.
        /// </summary>
        /// <returns>User profile data.</returns>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest(new { message = "Invalid user identifier in token." });
            }

            var user = await _userService.GetProfileAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.AvatarUrl,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            });
        }

        /// <summary>
        /// Changes the authenticated user's password.
        /// </summary>
        /// <param name="dto">Old and new password payload.</param>
        /// <returns>Operation status.</returns>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest(new { message = "Invalid user identifier in token." });
            }

            var changed = await _userService.ChangePasswordAsync(userId.Value, dto.OldPassword, dto.NewPassword);
            if (!changed)
            {
                return BadRequest(new { message = "Old password is incorrect or user does not exist." });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Soft-deactivates the authenticated user's account.
        /// </summary>
        /// <returns>Operation status.</returns>
        [Authorize]
        [HttpDelete("deactivate")]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest(new { message = "Invalid user identifier in token." });
            }

            var deactivated = await _userService.DeactivateAccountAsync(userId.Value);
            if (!deactivated)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Account deactivated successfully" });
        }

        /// <summary>
        /// Gets active users by role.
        /// </summary>
        /// <param name="role">Role filter (STUDENT, INSTRUCTOR, ADMIN).</param>
        /// <returns>Filtered users for the requested role.</returns>
        [Authorize]
        [HttpGet("by-role/{role}")]
        public async Task<IActionResult> GetByRole(string role)
        {
            try
            {
                var users = await _userService.GetAllByRoleAsync(role);
                if (users.Count == 0)
                {
                    return NotFound(new { message = "No users found for the requested role." });
                }

                return Ok(users.Select(user => new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.AvatarUrl,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLoginAt
                }));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Searches active users by full name or email.
        /// </summary>
        /// <param name="q">Search query text.</param>
        /// <returns>Matched users.</returns>
        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            var users = await _userService.SearchUsersAsync(q);
            if (users.Count == 0)
            {
                return NotFound(new { message = "No users matched the search criteria." });
            }

            return Ok(users.Select(user => new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.AvatarUrl,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            }));
        }

        /// <summary>
        /// Updates the authenticated user's profile fields and optional avatar.
        /// </summary>
        /// <param name="fullName">Updated full name.</param>
        /// <param name="avatar">Optional avatar file.</param>
        /// <returns>Operation status and avatar URL when uploaded.</returns>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] string fullName, IFormFile? avatar)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest(new { message = "Invalid user identifier in token." });
            }

            string? avatarUrl = null;

            if (avatar != null && avatar.Length > 0)
            {
                try
                {
                    using var stream = avatar.OpenReadStream();
                    avatarUrl = await _blobService.UploadFileAsync(stream, avatar.FileName, avatar.ContentType);
                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { message = "Avatar upload service is currently unavailable." });
                }
            }

            var result = await _userService.UpdateProfileAsync(userId.Value, fullName, avatarUrl);

            if (!result) return NotFound(new { message = "User not found" });

            return Ok(new { message = "Profile updated successfully", avatarUrl });
        }

        /// <summary>
        /// Safely extracts the numeric user id claim from the current JWT.
        /// </summary>
        /// <returns>User id when valid; otherwise null.</returns>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return null;
            }

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public record RegisterDto(string FullName, string Email, string Password, string Role);
    public record LoginDto(string Email, string Password);
    public record ValidateTokenDto(string Token);
    public record RefreshTokenDto(string RefreshToken);
}