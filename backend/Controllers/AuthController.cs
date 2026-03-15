using System.Security.Claims;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserStore _users;

    public AuthController(UserStore users)
    {
        _users = users;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error) = _users.Register(request.Username, request.Password);
        if (!success) return BadRequest(new { error });

        var user = _users.Authenticate(request.Username, request.Password);
        if (user == null) return StatusCode(500, new { error = "Failed to create account." });

        await SignInUser(user);
        return Ok(new UserInfoResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            CreatedAtUtc = user.CreatedAtUtc
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = _users.Authenticate(request.Username, request.Password);
        if (user == null) return Unauthorized(new { error = "Invalid username or password." });

        await SignInUser(user);
        return Ok(new UserInfoResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            CreatedAtUtc = user.CreatedAtUtc
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { error = "Not authenticated." });
        }

        var user = _users.FindById(userId);
        if (user == null) return Unauthorized(new { error = "Not authenticated." });

        return Ok(new UserInfoResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            CreatedAtUtc = user.CreatedAtUtc
        });
    }

    [HttpGet("history")]
    public IActionResult History()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { error = "Not authenticated." });
        }

        return Ok(_users.GetHistory(userId));
    }

    [HttpGet("history/{sessionId:guid}")]
    public IActionResult HistoryCase(Guid sessionId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { error = "Not authenticated." });
        }

        var entry = _users.GetHistoryCase(userId, sessionId);
        if (entry == null)
        {
            return NotFound(new { error = "Case history not found." });
        }

        return Ok(entry);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private async Task SignInUser(UserRecord user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }
}
