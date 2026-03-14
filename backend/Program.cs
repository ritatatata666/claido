using System.Collections.Concurrent;
using Claido.Models;
using Claido.Services;

var builder = WebApplication.CreateBuilder(args);
builder.LoadApiKey();

builder.Services.AddControllers();
builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, SessionState>>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? builder.Configuration["API_KEY"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    app.Logger.LogError("API_KEY is missing. Set API_KEY as an environment variable or in a .env file before calling AI-backed endpoints.");
}
else
{
    app.Logger.LogInformation("API_KEY is configured.");
}

app.UseCors();
app.MapControllers();
app.Run();
