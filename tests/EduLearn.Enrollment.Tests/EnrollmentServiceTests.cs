using EduLearn.Enrollment.API.Models;
using EnrollmentModel = EduLearn.Enrollment.API.Models.Enrollment;

namespace EduLearn.Enrollment.Tests;

public class EnrollmentServiceTests
{
    private EnrollmentModel _enrollment = null!;

    [SetUp]
    public void SetUp()
    {
        _enrollment = new EnrollmentModel
        {
            StudentId = 1001,
            CourseId = 2002
        };
    }

    [Test]
    public void ProjectReference_ShouldAllowAccessToEnrollmentModel()
    {
        Assert.That(_enrollment.StudentId, Is.EqualTo(1001));
        Assert.That(_enrollment.CourseId, Is.EqualTo(2002));
        Assert.That(_enrollment.Status, Is.EqualTo(EnrollmentStatus.ACTIVE));
    }
}
