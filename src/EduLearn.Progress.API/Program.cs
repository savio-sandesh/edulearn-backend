using EduLearn.Progress.API.Consumers;
using EduLearn.Progress.API.Data;
using EduLearn.Progress.API.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;
using System.Text;

Environment.SetEnvironmentVariable("MT_LICENSE", "Discord");
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProgressDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddHttpClient();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<CourseCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });

        cfg.ConfigureEndpoints(context);
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = GetRequiredJwtValue(jwtSettings, "Key");
if (jwtKey.StartsWith("<set-via-", StringComparison.Ordinal) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");
}

var jwtIssuer = GetRequiredJwtValue(jwtSettings, "Issuer");
var jwtAudience = GetRequiredJwtValue(jwtSettings, "Audience");

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
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only (without the Bearer prefix)."
    };

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
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var configuredOutputDirectory = builder.Configuration["Certificate:OutputDirectory"];
var certificatesOutputDirectory = string.IsNullOrWhiteSpace(configuredOutputDirectory)
    ? @"C:\Temp"
    : configuredOutputDirectory;

if (!Path.IsPathRooted(certificatesOutputDirectory))
{
    certificatesOutputDirectory = Path.Combine(app.Environment.ContentRootPath, certificatesOutputDirectory);
}

Directory.CreateDirectory(certificatesOutputDirectory);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(certificatesOutputDirectory),
    RequestPath = "/certificates"
});
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
