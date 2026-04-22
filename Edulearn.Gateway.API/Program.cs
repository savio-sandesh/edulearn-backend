var builder = WebApplication.CreateBuilder(args);

// YARP configuration load karo
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Gateway check ke liye ek simple endpoint (Optional)
app.MapGet("/", () => "EduLearn API Gateway is Running!");

// YARP Middleware enable karo
app.MapReverseProxy();

app.Run();