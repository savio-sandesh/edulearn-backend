using EduLearn.Auth.API.Models;
using EduLearn.Auth.API.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EduLearn.Auth.API.Services
{
    /// <summary>
    /// Implements authentication and profile business logic.
    /// </summary>
    public class UserService : IUserService
    {
        private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
        {
            "STUDENT",
            "INSTRUCTOR",
            "ADMIN"
        };

        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the user service.
        /// </summary>
        /// <param name="userRepository">User data access repository.</param>
        /// <param name="config">Application configuration source.</param>
        public UserService(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
            _passwordHasher = new PasswordHasher<User>();
        }

        /// <inheritdoc />
        public async Task<User> RegisterAsync(User user, string password)
        {
            user.Role = NormalizeRole(user.Role);
            user.Email = user.Email.Trim();

            var normalizedEmail = NormalizeEmail(user.Email);
            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            {
                throw new ArgumentException("Email is already registered.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return user;
        }

        /// <inheritdoc />
        public async Task<AuthTokensResult?> LoginAsync(string email, string password)
        {
            var normalizedEmail = NormalizeEmail(email);
            var user = await _userRepository.FindByEmailAsync(normalizedEmail, onlyActive: true);

            if (user == null) return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result != PasswordVerificationResult.Success) return null;

            await _userRepository.UpdateLastLoginAsync(user.UserId, DateTime.UtcNow);

            var refreshDurationDays = GetRefreshTokenDurationInDays();
            var rawRefreshToken = GenerateSecureToken();
            var refreshTokenHash = ComputeTokenHash(rawRefreshToken);

            await _userRepository.AddRefreshTokenAsync(new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDurationDays)
            });

            await _userRepository.SaveChangesAsync();

            var accessToken = GenerateJwtToken(user, out var accessTokenExpiresAt);
            return new AuthTokensResult
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt
            };
        }

        /// <inheritdoc />
        public async Task<AuthTokensResult?> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var rawRefreshToken = refreshToken.Trim();
            var refreshTokenHash = ComputeTokenHash(rawRefreshToken);

            var existingRefreshToken = await _userRepository.FindValidRefreshTokenAsync(refreshTokenHash, DateTime.UtcNow);
            if (existingRefreshToken == null)
            {
                return null;
            }

            var newRawRefreshToken = GenerateSecureToken();
            var newRefreshTokenHash = ComputeTokenHash(newRawRefreshToken);
            var refreshDurationDays = GetRefreshTokenDurationInDays();

            existingRefreshToken.RevokedAt = DateTime.UtcNow;
            existingRefreshToken.RevokedReason = "Rotated";
            existingRefreshToken.ReplacedByTokenHash = newRefreshTokenHash;

            await _userRepository.AddRefreshTokenAsync(new RefreshToken
            {
                UserId = existingRefreshToken.UserId,
                TokenHash = newRefreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDurationDays)
            });

            await _userRepository.SaveChangesAsync();

            var accessToken = GenerateJwtToken(existingRefreshToken.User, out var accessTokenExpiresAt);
            return new AuthTokensResult
            {
                AccessToken = accessToken,
                RefreshToken = newRawRefreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt
            };
        }

        /// <inheritdoc />
        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId, onlyActive: true, asNoTracking: true);
            return user != null;
        }

        /// <inheritdoc />
        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var rawToken = token.Trim();
            if (rawToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                rawToken = rawToken[7..].Trim();
            }

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return false;
            }

            var jwtSettings = _config.GetSection("Jwt");
            var jwtKey = GetRequiredJwtValue(jwtSettings, "Key");
            var jwtIssuer = GetRequiredJwtValue(jwtSettings, "Issuer");
            var jwtAudience = GetRequiredJwtValue(jwtSettings, "Audience");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(rawToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<User?> GetProfileAsync(int userId)
        {
            return await _userRepository.FindByUserIdAsync(userId, onlyActive: true, asNoTracking: true);
        }

        /// <inheritdoc />
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _userRepository.FindByUserIdAsync(userId, onlyActive: true, asNoTracking: false);
            if (user == null) return false;

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword);
            if (verifyResult != PasswordVerificationResult.Success) return false;

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> DeactivateAccountAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId, onlyActive: true, asNoTracking: false);
            if (user == null) return false;

            user.IsActive = false;
            await _userRepository.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<User>> GetAllByRoleAsync(string role)
        {
            var normalizedRole = NormalizeRole(role);
            return await _userRepository.FindAllByRoleAsync(normalizedRole);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<User>> SearchUsersAsync(string searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedTerm = searchTerm.Trim().ToUpperInvariant();
                return await _userRepository.SearchUsersAsync(normalizedTerm);
            }

            return await _userRepository.FindAllActiveAsync();
        }

        /// <inheritdoc />
        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string? avatarUrl)
        {
            var user = await _userRepository.FindByUserIdAsync(userId, onlyActive: true, asNoTracking: false);
            if (user == null) return false;

            user.FullName = fullName;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
            }

            await _userRepository.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Generates a signed JWT token for an authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <returns>A signed JWT token string.</returns>
        private string GenerateJwtToken(User user, out DateTime accessTokenExpiresAt)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var jwtKey = GetRequiredJwtValue(jwtSettings, "Key");
            var jwtIssuer = GetRequiredJwtValue(jwtSettings, "Issuer");
            var jwtAudience = GetRequiredJwtValue(jwtSettings, "Audience");
            var durationRaw = GetRequiredJwtValue(jwtSettings, "DurationInMinutes");

            if (!int.TryParse(durationRaw, out var durationInMinutes) || durationInMinutes <= 0)
            {
                throw new InvalidOperationException("Jwt:DurationInMinutes must be a positive integer.");
            }

            var key = Encoding.ASCII.GetBytes(jwtKey);
            accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(durationInMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role), // Drives [Authorize(Roles)] attribute
                new Claim("FullName", user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = accessTokenExpiresAt,
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Reads a required Jwt setting and fails fast when missing.
        /// </summary>
        /// <param name="jwtSection">Jwt configuration section.</param>
        /// <param name="key">Configuration key name inside Jwt section.</param>
        /// <returns>Non-empty configuration value.</returns>
        private static string GetRequiredJwtValue(IConfigurationSection jwtSection, string key)
        {
            var value = jwtSection[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Jwt:{key} is missing from configuration.");
            }

            return value;
        }

        private static string NormalizeRole(string role)
        {
            var normalizedRole = role?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!AllowedRoles.Contains(normalizedRole))
            {
                throw new ArgumentException("Role must be STUDENT, INSTRUCTOR, or ADMIN.");
            }

            return normalizedRole;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private int GetRefreshTokenDurationInDays()
        {
            var jwtSettings = _config.GetSection("Jwt");
            var value = jwtSettings["RefreshTokenDurationInDays"];

            if (string.IsNullOrWhiteSpace(value))
            {
                return 7;
            }

            if (!int.TryParse(value, out var refreshTokenDurationInDays) || refreshTokenDurationInDays <= 0)
            {
                throw new InvalidOperationException("Jwt:RefreshTokenDurationInDays must be a positive integer.");
            }

            return refreshTokenDurationInDays;
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string ComputeTokenHash(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}