using EduLearn.Assessment.API.Data;
using EduLearn.Assessment.API.Repositories;
using EduLearn.Assessment.API.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

Environment.SetEnvironmentVariable("MT_LICENSE", "Discord");

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Configuration with Azure Resiliency ---
builder.Services.AddDbContext<AssessmentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizService, QuizService>();

// --- 2. MassTransit Messaging Setup ---
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitUri = new Uri(builder.Configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("RabbitMQ:Host is missing from configuration."));
        cfg.Host(rabbitUri);

        cfg.ConfigureEndpoints(context);
    });
});

// --- 3. JWT Authentication Logic ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = GetRequiredJwtValue(jwtSettings, "Key");

if (jwtKey.StartsWith("<set-via-", StringComparison.Ordinal) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("CRITICAL: Jwt:Key must be configured in User Secrets and be at least 32 chars.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();

// --- 4. Swagger Configuration ---
builder.Services.AddSwaggerGen(options =>
{
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only."
    };

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Assessment API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", jwtScheme);

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 5. Automated Migration Execution ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AssessmentDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("INFO: Applying Assessment API migrations to Azure SQL...");
            context.Database.Migrate();
            Console.WriteLine("SUCCESS: Assessment Database Migrated Successfully.");
        }
        else
        {
            Console.WriteLine("INFO: Assessment Database is already up to date.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: Migration failed. Details: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string GetRequiredJwtValue(IConfigurationSection section, string key)
{
    var value = section[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Jwt:{key} is missing from configuration.");
    }
    return value;
}