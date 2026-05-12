using System.Net;
using System.Reflection;
using EduLearn.Progress.API.Controllers;
using EduLearn.Progress.API.Data;
using EduLearn.Progress.API.Models;
using EduLearn.Progress.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduLearn.Progress.Tests;

public class ProgressApiCoreTests
{
    [Test]
    public async Task CertificatePersistenceTest_WritesRecordToDatabase()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProgressDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ProgressDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var certificate = new Certificate
        {
            StudentId = 101,
            CourseId = 501,
            CertificateUrl = "/certificates/cert-101-501.pdf",
            IssuedAt = DateTime.UtcNow,
            VerificationCode = Guid.NewGuid().ToString("N").ToUpperInvariant()
        };

        await context.Certificates.AddAsync(certificate);
        await context.SaveChangesAsync();

        var saved = await context.Certificates.FirstOrDefaultAsync(x => x.StudentId == 101 && x.CourseId == 501);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.CertificateUrl, Is.EqualTo("/certificates/cert-101-501.pdf"));
        Assert.That(saved.IssuedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task AvatarDownloadLogicTest_AttemptsDownloadAndFallsBackOnFailure()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var webRoot = Path.Combine(tempRoot, "wwwroot");
            var imagesDir = Path.Combine(webRoot, "images");
            Directory.CreateDirectory(imagesDir);

            var fallbackBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7ZJ9sAAAAASUVORK5CYII=");
            var fallbackPath = Path.Combine(imagesDir, "default-avatar.png");
            await File.WriteAllBytesAsync(fallbackPath, fallbackBytes);

            var outputDir = Path.Combine(tempRoot, "certificates");
            Directory.CreateDirectory(outputDir);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Certificate:OutputDirectory"] = outputDir
                })
                .Build();

            var environment = new FakeWebHostEnvironment
            {
                ContentRootPath = tempRoot,
                WebRootPath = webRoot
            };

            var successHandler = new CountingHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
                };

                return response;
            });

            var successFactory = new Mock<IHttpClientFactory>();
            successFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(successHandler));

            var successService = new CertificateService(
                environment,
                config,
                successFactory.Object,
                Mock.Of<IBlobStorageService>(),
                Mock.Of<ILogger<CertificateService>>());

            var getAvatarBytesMethod = typeof(CertificateService)
                .GetMethod("GetAvatarBytesAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(getAvatarBytesMethod, Is.Not.Null);

            var successTask = (Task<byte[]>)getAvatarBytesMethod!.Invoke(successService, new object?[] { 11, "https://blob.test/avatar.png" })!;
            var downloadedBytes = await successTask;

            Assert.That(successHandler.RequestCount, Is.EqualTo(1));
            Assert.That(downloadedBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));

            var failHandler = new CountingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var failFactory = new Mock<IHttpClientFactory>();
            failFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(failHandler));

            var failService = new CertificateService(
                environment,
                config,
                failFactory.Object,
                Mock.Of<IBlobStorageService>(),
                Mock.Of<ILogger<CertificateService>>());

            var failTask = (Task<byte[]>)getAvatarBytesMethod.Invoke(failService, new object?[] { 12, "https://blob.test/fail.png" })!;
            var fallbackResultBytes = await failTask;

            Assert.That(failHandler.RequestCount, Is.EqualTo(1));
            Assert.That(fallbackResultBytes, Is.EqualTo(fallbackBytes));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public async Task FilePathLogicTest_GeneratedCertificateUrlUsesExpectedRelativeFormat()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var webRoot = Path.Combine(tempRoot, "wwwroot");
            var imagesDir = Path.Combine(webRoot, "images");
            Directory.CreateDirectory(imagesDir);

            var fallbackBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7ZJ9sAAAAASUVORK5CYII=");
            await File.WriteAllBytesAsync(Path.Combine(imagesDir, "default-avatar.png"), fallbackBytes);

            var outputDir = Path.Combine(tempRoot, "certificates");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Certificate:OutputDirectory"] = outputDir
                })
                .Build();

            var environment = new FakeWebHostEnvironment
            {
                ContentRootPath = tempRoot,
                WebRootPath = webRoot
            };

            var factory = new Mock<IHttpClientFactory>();
            factory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(new CountingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));

            var service = new CertificateService(
                environment,
                config,
                factory.Object,
                Mock.Of<IBlobStorageService>(),
                Mock.Of<ILogger<CertificateService>>());

            var url = await service.GenerateCertificateAsync(
                studentId: 555,
                courseId: 777,
                verificationCode: "VCODE123",
                issuedAtUtc: DateTime.UtcNow,
                studentName: "Test Student",
                avatarUrl: "https://blob.test/not-found.png");

            Assert.That(url, Does.StartWith("/certificates/"));
            Assert.That(url, Does.EndWith(".pdf"));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public async Task DownloadEndpointLogic_ReturnsPhysicalFileResult_ForValidId()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProgressDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ProgressDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var tempRoot = CreateTempDirectory();
        try
        {
            var outputDir = Path.Combine(tempRoot, "certificates");
            Directory.CreateDirectory(outputDir);

            var fileName = "cert-201-901.pdf";
            var filePath = Path.Combine(outputDir, fileName);
            await File.WriteAllBytesAsync(filePath, new byte[] { 37, 80, 68, 70 });

            var certificate = new Certificate
            {
                StudentId = 201,
                CourseId = 901,
                CertificateUrl = $"/certificates/{fileName}",
                IssuedAt = DateTime.UtcNow,
                VerificationCode = Guid.NewGuid().ToString("N").ToUpperInvariant()
            };

            await context.Certificates.AddAsync(certificate);
            await context.SaveChangesAsync();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Certificate:OutputDirectory"] = outputDir
                })
                .Build();

            var environment = new FakeWebHostEnvironment
            {
                ContentRootPath = tempRoot,
                WebRootPath = Path.Combine(tempRoot, "wwwroot")
            };

            var controller = new ProgressController(context);

            var result = await controller.DownloadCertificate(certificate.Id);

            Assert.That(result, Is.TypeOf<PhysicalFileResult>());

            var physicalFileResult = (PhysicalFileResult)result;
            Assert.That(physicalFileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(physicalFileResult.FileDownloadName, Is.EqualTo(fileName));
            Assert.That(physicalFileResult.FileName, Is.EqualTo(filePath));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public async Task MarkCompleteEndpoint_UpsertsAndRecalculatesProgressPercent()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProgressDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ProgressDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await context.LessonProgress.AddAsync(new LessonProgress
        {
            StudentId = 700,
            CourseId = 800,
            LessonId = 1,
            IsCompleted = false,
            CompletedAt = null,
            ProgressPercent = 0m
        });
        await context.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var environment = new FakeWebHostEnvironment
        {
            ContentRootPath = CreateTempDirectory(),
            WebRootPath = Path.Combine(Path.GetTempPath(), "wwwroot")
        };

        var controller = new ProgressController(context);

        var firstResult = await controller.MarkComplete(new ProgressController.MarkCompleteRequest
        {
            StudentId = 700,
            CourseId = 800,
            LessonId = 1,
            IsCompleted = true
        });

        Assert.That(firstResult, Is.TypeOf<OkObjectResult>());

        var firstRecord = await context.LessonProgress.SingleAsync(x =>
            x.StudentId == 700 && x.CourseId == 800 && x.LessonId == 1);
        Assert.That(firstRecord.IsCompleted, Is.True);
        Assert.That(firstRecord.ProgressPercent, Is.EqualTo(100m));

        var secondResult = await controller.MarkComplete(new ProgressController.MarkCompleteRequest
        {
            StudentId = 700,
            CourseId = 800,
            LessonId = 2,
            IsCompleted = false
        });

        Assert.That(secondResult, Is.TypeOf<OkObjectResult>());

        var records = await context.LessonProgress
            .Where(x => x.StudentId == 700 && x.CourseId == 800)
            .OrderBy(x => x.LessonId)
            .ToListAsync();

        Assert.That(records, Has.Count.EqualTo(2));
        Assert.That(records[0].ProgressPercent, Is.EqualTo(50m));
        Assert.That(records[1].ProgressPercent, Is.EqualTo(50m));

        var okPayload = (OkObjectResult)secondResult;
        var payloadProgressPercent = ReadAnonymousDecimalProperty(okPayload.Value!, "progressPercent");
        Assert.That(payloadProgressPercent, Is.EqualTo(50m));
    }

    [Test]
    public async Task VerifyCertificateEndpoint_ValidatesGuidAndFindsCertificate()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProgressDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ProgressDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var verificationGuid = Guid.NewGuid();
        var certificate = new Certificate
        {
            StudentId = 300,
            CourseId = 400,
            CertificateUrl = "/certificates/cert-300-400.pdf",
            IssuedAt = DateTime.UtcNow,
            VerificationCode = verificationGuid.ToString("N").ToUpperInvariant()
        };

        await context.Certificates.AddAsync(certificate);
        await context.SaveChangesAsync();

        var controller = new ProgressController(context);

        var invalidGuidResult = await controller.VerifyCertificate("not-a-guid");
        Assert.That(invalidGuidResult, Is.TypeOf<BadRequestObjectResult>());

        var missingResult = await controller.VerifyCertificate(Guid.NewGuid().ToString());
        Assert.That(missingResult, Is.TypeOf<NotFoundObjectResult>());

        var foundResult = await controller.VerifyCertificate(verificationGuid.ToString("D"));
        Assert.That(foundResult, Is.TypeOf<OkObjectResult>());

        var payload = (OkObjectResult)foundResult;
        var valid = ReadAnonymousBoolProperty(payload.Value!, "valid");
        var studentId = ReadAnonymousIntProperty(payload.Value!, "StudentId");
        var courseId = ReadAnonymousIntProperty(payload.Value!, "CourseId");

        Assert.That(valid, Is.True);
        Assert.That(studentId, Is.EqualTo(300));
        Assert.That(courseId, Is.EqualTo(400));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "edulearn-progress-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static decimal ReadAnonymousDecimalProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        var value = property!.GetValue(source);
        Assert.That(value, Is.Not.Null);
        return Convert.ToDecimal(value);
    }

    private static bool ReadAnonymousBoolProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        var value = property!.GetValue(source);
        Assert.That(value, Is.Not.Null);
        return Convert.ToBoolean(value);
    }

    private static int ReadAnonymousIntProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        var value = property!.GetValue(source);
        Assert.That(value, Is.Not.Null);
        return Convert.ToInt32(value);
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public int RequestCount { get; private set; }

        public CountingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "EduLearn.Progress.API.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
