using System.Collections.Concurrent;
using Claido.Models;
using Claido.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.LoadApiKey();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, SessionState>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, Guid>>();
builder.Services.AddSingleton<SessionCreator>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "claido_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost")
              .AllowCredentials()
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
