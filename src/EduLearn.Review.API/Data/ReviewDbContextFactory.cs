using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduLearn.Review.API.Data;

public class ReviewDbContextFactory : IDesignTimeDbContextFactory<ReviewDbContext>
{
    public ReviewDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<ReviewDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ReviewDbContext(optionsBuilder.Options);
    }
}
