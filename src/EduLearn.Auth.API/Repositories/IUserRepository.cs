using EduLearn.Auth.API.Models;

namespace EduLearn.Auth.API.Repositories
{
    /// <summary>
    /// Defines data access operations for user entities.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string normalizedEmail, bool onlyActive = true);
        Task<User?> FindByUserIdAsync(int userId, bool onlyActive = true, bool asNoTracking = false);
        Task<bool> ExistsByEmailAsync(string normalizedEmail);
        Task<IReadOnlyList<User>> FindAllByRoleAsync(string normalizedRole, bool includeInactive = false);
        Task<IReadOnlyList<User>> FindAllActiveAsync();
        Task UpdateLastLoginAsync(int userId, DateTime lastLoginAtUtc);
        Task<IReadOnlyList<User>> SearchUsersAsync(string normalizedSearchTerm);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> FindValidRefreshTokenAsync(string tokenHash, DateTime utcNow);
        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
}
