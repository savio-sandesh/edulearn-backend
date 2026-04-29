using EduLearn.Auth.API.Data;
using EduLearn.Auth.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Auth.API.Repositories
{
    /// <summary>
    /// EF Core implementation of user data access.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindByEmailAsync(string normalizedEmail, bool onlyActive = true)
        {
            var query = _context.Users.AsQueryable();

            if (onlyActive)
            {
                query = query.Where(u => u.IsActive);
            }

            return await query.FirstOrDefaultAsync(u => u.Email.ToUpper() == normalizedEmail);
        }

        public async Task<User?> FindByUserIdAsync(int userId, bool onlyActive = true, bool asNoTracking = false)
        {
            var query = _context.Users.AsQueryable();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (onlyActive)
            {
                query = query.Where(u => u.IsActive);
            }

            return await query.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> ExistsByEmailAsync(string normalizedEmail)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToUpper() == normalizedEmail);
        }

        public async Task<IReadOnlyList<User>> FindAllByRoleAsync(string normalizedRole, bool includeInactive = false)
        {
            var query = _context.Users.AsNoTracking().Where(u => u.Role == normalizedRole);
            
            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            return await query.OrderBy(u => u.FullName).ToListAsync();
        }

        public async Task<IReadOnlyList<User>> FindAllActiveAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task UpdateLastLoginAsync(int userId, DateTime lastLoginAtUtc)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return;
            }

            user.LastLoginAt = lastLoginAtUtc;
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<User>> SearchUsersAsync(string normalizedSearchTerm)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive &&
                    (u.FullName.ToUpper().Contains(normalizedSearchTerm) ||
                     u.Email.ToUpper().Contains(normalizedSearchTerm)))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task<RefreshToken?> FindValidRefreshTokenAsync(string tokenHash, DateTime utcNow)
        {
            return await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.TokenHash == tokenHash &&
                    r.RevokedAt == null &&
                    r.ExpiresAt > utcNow &&
                    r.User.IsActive);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
