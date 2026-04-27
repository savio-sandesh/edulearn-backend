using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS POLICY CONFIGURATION
// This allows the Angular frontend (running on port 4200) to communicate with the Gateway.
// Without this, the browser will block all cross-origin requests for security reasons.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyMethod()                     // Allows GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader()                     // Allows custom headers like 'Authorization'
              .AllowCredentials();                  // Necessary if you plan to use Cookies or SignalR
    });
});

// 2. JWT AUTHENTICATION SETUP
// The Gateway validates the token before forwarding the request to microservices.
// It uses the Shared Key, Issuer, and Audience defined in appsettings.json.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Registers authorization services to handle role-based access control.
builder.Services.AddAuthorization();

// 3. YARP REVERSE PROXY CONFIGURATION
// Loads routing rules (Routes and Clusters) from the 'ReverseProxy' section in appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 4. MIDDLEWARE PIPELINE (Order is critical)

// Health check endpoint to verify if the Gateway is active.
app.MapGet("/", () => "EduLearn API Gateway is Running with Security & CORS!");

// Step A: Enable CORS first to handle 'Pre-flight' OPTIONS requests from the browser.
app.UseCors("AngularFrontendPolicy");

// Step B: Authenticate the user based on the JWT token.
app.UseAuthentication();

// Step C: Authorize the user based on their roles (Student/Admin).
app.UseAuthorization();

// Step D: Map the YARP Reverse Proxy to forward requests to the appropriate microservices.
app.MapReverseProxy();

app.Run();