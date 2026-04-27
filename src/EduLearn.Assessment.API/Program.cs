using EduLearn.Assessment.API.Data;
using EduLearn.Assessment.API.Repositories;
using EduLearn.Assessment.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AssessmentDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizService, QuizService>();

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
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

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
