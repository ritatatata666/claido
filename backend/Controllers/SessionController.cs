using System.Collections.Concurrent;
using System.Security.Claims;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly SessionCreator _sessionCreator;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;

    public SessionController(
        SessionCreator sessionCreator,
        ConcurrentDictionary<Guid, SessionState> sessions)
    {
        _sessionCreator = sessionCreator;
        _sessions = sessions;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSession()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { error = "Not authenticated." });

        try
        {
            var session = await _sessionCreator.CreateSessionAsync();
            session.OwnerUserId = userId;
            session.StartedAtUtc = DateTime.UtcNow;
            _sessions[session.SessionId] = session;

            return Ok(SessionResponder.Build(session));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
