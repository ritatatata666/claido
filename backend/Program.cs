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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run();
