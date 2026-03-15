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
    private readonly UserStore _users;

    public SessionController(
        SessionCreator sessionCreator,
        ConcurrentDictionary<Guid, SessionState> sessions,
        UserStore users)
    {
        _sessionCreator = sessionCreator;
        _sessions = sessions;
        _users = users;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest? request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { error = "Not authenticated." });

        try
        {
            var session = await _sessionCreator.CreateSessionAsync();
            session.InvestigatorName = string.IsNullOrWhiteSpace(request?.DisplayName)
                ? "Player"
                : request!.DisplayName.Trim();
            session.OwnerUserId = userId;
            session.StartedAtUtc = DateTime.UtcNow;
            _sessions[session.SessionId] = session;

            return Ok(new
            {
                sessionId = session.SessionId,
                investigatorName = session.InvestigatorName,
                culprit = new { session.Culprit.Id, session.Culprit.Name, session.Culprit.Department, session.Culprit.Role },                
                employees = session.Employees,
                incidentTimestamp = session.IncidentTimestamp,
                badgeDiscrepancy = session.BadgeDiscrepancy,
                motive = session.Motive,
                vaultCode = session.VaultCode,
                vaultWord1 = session.VaultWord1,
                vaultWord2 = session.VaultWord2,
                vaultWord3 = session.VaultWord3,
                vaultWord4 = session.VaultWord4
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public IActionResult GetLeaderboard()
    {
        var entries = _users.GetLeaderboard(5)
            .Select(entry => new
            {
                displayName = entry.DisplayName,
                solveSeconds = entry.SolveSeconds,
                completedAtUtc = entry.CompletedAtUtc,
            });

        return Ok(entries);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
