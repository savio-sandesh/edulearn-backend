using System.IdentityModel.Tokens.Jwt;
using EduLearn.Auth.API.Models;
using EduLearn.Auth.API.Repositories;
using EduLearn.Auth.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EduLearn.Auth.Tests;

public class UserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _service = new UserService(_userRepositoryMock.Object, BuildJwtConfiguration());
    }

    [Test]
    public async Task RegisterAsync_WhenInputIsValid_HashesPasswordAndCreatesUser()
    {
        // Arrange
        var user = new User
        {
            FullName = "Test User",
            Email = "test.user@edulearn.com",
            Role = "student"
        };

        User? addedUser = null;

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync("TEST.USER@EDULEARN.COM"))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => addedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RegisterAsync(user, "Pass@123");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Role, Is.EqualTo("STUDENT"));
        Assert.That(result.PasswordHash, Is.Not.Null.And.Not.Empty);
        Assert.That(result.PasswordHash, Is.Not.EqualTo("Pass@123"));

        var hasher = new PasswordHasher<User>();
        var verify = hasher.VerifyHashedPassword(result, result.PasswordHash, "Pass@123");
        Assert.That(verify, Is.EqualTo(PasswordVerificationResult.Success));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        Assert.That(addedUser, Is.SameAs(result));
    }

    [Test]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsArgumentException()
    {
        // Arrange
        var user = new User
        {
            FullName = "Existing User",
            Email = "existing@edulearn.com",
            Role = "STUDENT"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync("EXISTING@EDULEARN.COM"))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.RegisterAsync(user, "Pass@123");

        // Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await act());
        Assert.That(ex!.Message, Does.Contain("Email is already registered."));
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsAreCorrect_ReturnsJwtTokenResponse()
    {
        // Arrange
        var user = new User
        {
            UserId = 41,
            FullName = "Valid User",
            Email = "valid.user@edulearn.com",
            Role = "STUDENT",
            IsActive = true
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "Pass@123");

        _userRepositoryMock
            .Setup(x => x.FindByEmailAsync("VALID.USER@EDULEARN.COM", true))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.UpdateLastLoginAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.LoginAsync("valid.user@edulearn.com", "Pass@123");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.AccessTokenExpiresAt, Is.GreaterThan(DateTime.UtcNow));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.That(jwt.Issuer, Is.EqualTo("EduLearnAuthAPI"));
        Assert.That(jwt.Audiences, Does.Contain("EduLearnAngularClient"));

        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(user.UserId, It.IsAny<DateTime>()), Times.Once);
        _userRepositoryMock.Verify(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordDoesNotMatch_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            UserId = 51,
            FullName = "User",
            Email = "user@edulearn.com",
            Role = "STUDENT",
            IsActive = true
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "CorrectPass@123");

        _userRepositoryMock
            .Setup(x => x.FindByEmailAsync("USER@EDULEARN.COM", true))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync("user@edulearn.com", "WrongPass@123");

        // Assert
        Assert.That(result, Is.Null);
        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _userRepositoryMock.Verify(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static IConfiguration BuildJwtConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "this-is-a-very-long-test-key-for-jwt-123456",
            ["Jwt:Issuer"] = "EduLearnAuthAPI",
            ["Jwt:Audience"] = "EduLearnAngularClient",
            ["Jwt:DurationInMinutes"] = "60",
            ["Jwt:RefreshTokenDurationInDays"] = "7"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
