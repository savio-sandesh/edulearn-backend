using EduLearn.Auth.API.Models;

namespace EduLearn.Auth.API.Services
{
    public interface IUserService
{
    Task<User> RegisterAsync(User user, string password);
    Task<string?> LoginAsync(string email, string password);
    // Naya method
    Task<bool> UpdateProfileAsync(int userId, string fullName, string? avatarUrl);
}
}