using System.Collections.Concurrent;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session")]
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
        try
        {
            var session = await _sessionCreator.CreateSessionAsync();
            _sessions[session.SessionId] = session;

            return Ok(SessionResponder.Build(session));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
