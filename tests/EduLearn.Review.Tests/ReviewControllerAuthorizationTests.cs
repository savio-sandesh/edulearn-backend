using EduLearn.Review.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EduLearn.Review.Tests;

public class ReviewControllerAuthorizationTests
{
    [Test]
    public void ApproveReviewEndpoint_RequiresAdminRole()
    {
        // Arrange
        var method = typeof(ReviewController).GetMethod(nameof(ReviewController.ApproveReview));

        // Assert
        Assert.That(method, Is.Not.Null);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.That(authorize, Is.Not.Null);
        Assert.That(authorize!.Roles, Is.EqualTo("ADMIN"));
    }

    [Test]
    public void AddReviewEndpoint_RequiresStudentRole()
    {
        // Arrange
        var method = typeof(ReviewController).GetMethod(nameof(ReviewController.AddReview));

        // Assert
        Assert.That(method, Is.Not.Null);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.That(authorize, Is.Not.Null);
        Assert.That(authorize!.Roles, Is.EqualTo("STUDENT"));
    }
}
