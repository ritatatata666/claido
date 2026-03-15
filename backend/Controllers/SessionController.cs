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

            return Ok(new
            {
                sessionId = session.SessionId,
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
}
