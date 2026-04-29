using EduLearn.Auth.API.Models;

namespace EduLearn.Auth.API.Services
{
    /// <summary>
    /// Defines business operations for user authentication and profile management.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user with a hashed password.
        /// </summary>
        /// <param name="user">The user entity to create.</param>
        /// <param name="password">The raw password to hash and store.</param>
        /// <returns>The created user entity.</returns>
        Task<User> RegisterAsync(User user, string password);

        /// <summary>
        /// Validates credentials and returns a JWT token when successful.
        /// </summary>
        /// <param name="email">The login email.</param>
        /// <param name="password">The raw password to verify.</param>
        /// <returns>Access/refresh tokens when successful; otherwise null.</returns>
        Task<AuthTokensResult?> LoginAsync(string email, string password);

        /// <summary>
        /// Exchanges a valid refresh token for a new token pair and rotates refresh-token state.
        /// </summary>
        /// <param name="refreshToken">Raw refresh token.</param>
        /// <returns>New access/refresh tokens when successful; otherwise null.</returns>
        Task<AuthTokensResult?> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Performs logout handling for an active user.
        /// </summary>
        /// <param name="userId">The user id from claims.</param>
        /// <returns>True when logout can be accepted; otherwise false.</returns>
        Task<bool> LogoutAsync(int userId);

        /// <summary>
        /// Validates a JWT token for signature, issuer, audience, and lifetime.
        /// </summary>
        /// <param name="token">Raw JWT token (with or without Bearer prefix).</param>
        /// <returns>True when token is valid; otherwise false.</returns>
        bool ValidateToken(string token);

        /// <summary>
        /// Retrieves the active user profile for a given user id.
        /// </summary>
        /// <param name="userId">The user id from claims.</param>
        /// <returns>The user profile when found; otherwise null.</returns>
        Task<User?> GetProfileAsync(int userId);

        /// <summary>
        /// Changes the password for an active user after verifying the old password.
        /// </summary>
        /// <param name="userId">The user id from claims.</param>
        /// <param name="oldPassword">The current password for verification.</param>
        /// <param name="newPassword">The new password to hash and store.</param>
        /// <returns>True if password changed; otherwise false.</returns>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>
        /// Soft-deactivates an account by setting IsActive to false.
        /// </summary>
        /// <param name="userId">The user id to deactivate.</param>
        /// <returns>True if deactivated; otherwise false.</returns>
        Task<bool> DeactivateAccountAsync(int userId);

        /// <summary>
        /// Gets all active users for a given role.
        /// </summary>
        /// <param name="role">Role filter (STUDENT, INSTRUCTOR, ADMIN).</param>
        /// <param name="includeInactive">Whether to include inactive users.</param>
        /// <returns>List of users in the role.</returns>
        Task<IReadOnlyList<User>> GetAllByRoleAsync(string role, bool includeInactive = false);

        /// <summary>
        /// Toggles the IsActive status of a user.
        /// </summary>
        /// <param name="userId">The user id to toggle.</param>
        /// <returns>True if toggled; otherwise false.</returns>
        Task<bool> ToggleUserStatusAsync(int userId);

        /// <summary>
        /// Searches active users by full name or email.
        /// </summary>
        /// <param name="searchTerm">Search term for name or email.</param>
        /// <returns>List of matched active users.</returns>
        Task<IReadOnlyList<User>> SearchUsersAsync(string searchTerm);

        /// <summary>
        /// Updates profile fields for an existing user.
        /// </summary>
        /// <param name="userId">The user id to update.</param>
        /// <param name="fullName">The updated full name.</param>
        /// <param name="avatarUrl">Optional avatar URL to save.</param>
        /// <returns>True if update succeeded; otherwise false.</returns>
        Task<bool> UpdateProfileAsync(int userId, string fullName, string? avatarUrl);
    }
}