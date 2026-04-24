using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduLearn.Enrollment.API.Data;

public class EnrollmentDbContextFactory : IDesignTimeDbContextFactory<EnrollmentDbContext>
{
    public EnrollmentDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        var optionsBuilder = new DbContextOptionsBuilder<EnrollmentDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new EnrollmentDbContext(optionsBuilder.Options);
    }
}
