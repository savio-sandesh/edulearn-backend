using EduLearn.Auth.API.Data;
using EduLearn.Auth.API.Repositories;
using EduLearn.Auth.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Configuration (SQL Server) ---
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. Dependency Injection for Services ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBlobService, BlobService>(); // Moved here for better organization

// --- 3. JWT Authentication Setup ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = GetRequiredJwtValue(jwtSettings, "Key");
if (jwtKey.StartsWith("<set-via-", StringComparison.Ordinal) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");
}

var jwtIssuer = GetRequiredJwtValue(jwtSettings, "Issuer");
var jwtAudience = GetRequiredJwtValue(jwtSettings, "Audience");
var key = Encoding.ASCII.GetBytes(jwtKey);

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtAuth");

            logger.LogWarning(context.Exception, "JWT authentication failed.");
            return Task.CompletedTask;
        }
    };
});

// --- 4. Authorization & Controllers ---
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 5. Swagger Setup with Authorize Button ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Auth API", Version = "v1" });

    var bearerSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token only."
    };

    options.AddSecurityDefinition("Bearer", bearerSecurityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 6. Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

// Order is crucial: Auth first, then Authorization
app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Reads a required Jwt setting and fails fast when it is missing or blank.
/// </summary>
/// <param name="jwtSection">Jwt configuration section.</param>
/// <param name="key">Configuration key name inside Jwt section.</param>
/// <returns>Non-empty configuration value.</returns>
static string GetRequiredJwtValue(IConfigurationSection jwtSection, string key)
{
    var value = jwtSection[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Jwt:{key} is missing from configuration.");
    }

    return value;
}