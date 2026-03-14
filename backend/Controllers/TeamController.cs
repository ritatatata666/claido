using System;
using System.Collections.Concurrent;
using System.Linq;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session")]
public class TeamController : ControllerBase
{
    private readonly SessionCreator _sessionCreator;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;
    private readonly ConcurrentDictionary<string, Guid> _joinCodes;
    private readonly Random _rng = new();

    public TeamController(
        SessionCreator sessionCreator,
        ConcurrentDictionary<Guid, SessionState> sessions,
        ConcurrentDictionary<string, Guid> joinCodes)
    {
        _sessionCreator = sessionCreator;
        _sessions = sessions;
        _joinCodes = joinCodes;
    }

    [HttpPost("join")]
    public IActionResult Join([FromBody] JoinTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
            return BadRequest(new { error = "Join code is required." });

        var code = request.JoinCode.Trim().ToUpperInvariant();
        if (!_joinCodes.TryGetValue(code, out var sessionId) ||
            !_sessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(new { error = "Invalid join code." });
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? $"Player {session.TeamMembers.Count + 1}"
            : request.DisplayName.Trim();

        TeamMember member;
        lock (session)
        {
            member = new TeamMember
            {
                DisplayName = displayName,
                Role = DetermineRole(session),
                JoinedAt = DateTime.UtcNow
            };
            session.TeamMembers.Add(member);
            session.TeamMode = "team";
        }

        var response = SessionResponder.BuildTeam(session);
        response.PlayerId = member.MemberId;
        response.Role = member.Role;
        return Ok(response);
    }

    [HttpPost("team/create")]
    public async Task<IActionResult> CreateTeamRoom([FromBody] CreateTeamRoomRequest request)
    {
        try
        {
            var session = await _sessionCreator.CreateSessionAsync();
            session.TeamMode = "team";
            _sessions[session.SessionId] = session;

            var displayName = string.IsNullOrWhiteSpace(request?.DisplayName)
                ? "Host Investigator"
                : request.DisplayName.Trim();

            TeamMember member;

            lock (session)
            {
                member = new TeamMember
                {
                    DisplayName = displayName,
                    Role = DetermineRole(session),
                    JoinedAt = DateTime.UtcNow
                };
                session.TeamMembers.Add(member);
                session.TeamMode = "team";
            }

            var response = SessionResponder.BuildTeam(session);
            response.PlayerId = member.MemberId;
            response.Role = member.Role;
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{sessionId}/team/state")]
    public IActionResult State(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        var response = SessionResponder.BuildTeam(session);
        return Ok(response);
    }

    [HttpPost("{sessionId}/team/lock")]
    public IActionResult Lock(Guid sessionId, [FromBody] TeamClueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClueId))
            return BadRequest(new { error = "Clue id is required." });

        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        var member = session.TeamMembers.FirstOrDefault(m => m.MemberId == request.MemberId);
        if (member == null || member.Role != "villain")
            return BadRequest(new { error = "Only villains can lock clues." });

        IActionResult? failure = null;
        lock (session)
        {
            if (session.VillainTokens <= 0)
            {
                failure = BadRequest(new { error = "No sabotage tokens left." });
            }
            else if (session.LockedClues.Contains(request.ClueId))
            {
                failure = BadRequest(new { error = "Clue already locked." });
            }
            else
            {
                session.LockedClues.Add(request.ClueId);
                session.VillainTokens--;
                AddAction(session, new TeamActionEntry
                {
                    Actor = "villain",
                    Action = "lock",
                    ClueId = request.ClueId,
                    Room = request.Room,
                    Snippet = request.Snippet,
                    MemberId = member.MemberId,
                    DisplayName = member.DisplayName,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        if (failure != null)
        {
            return failure;
        }

        var response = SessionResponder.BuildTeam(session);
        response.PlayerId = member.MemberId;
        response.Role = member.Role;
        return Ok(response);
    }

    [HttpPost("{sessionId}/team/unlock")]
    public IActionResult Unlock(Guid sessionId, [FromBody] TeamClueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClueId))
            return BadRequest(new { error = "Clue id is required." });

        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        var member = session.TeamMembers.FirstOrDefault(m => m.MemberId == request.MemberId);
        if (member == null || member.Role != "good")
            return BadRequest(new { error = "Only investigators can unlock clues." });

        IActionResult? failure = null;
        lock (session)
        {
            if (session.GoodTokens <= 0)
            {
                failure = BadRequest(new { error = "No counter tokens left." });
            }
            else if (!session.LockedClues.Contains(request.ClueId))
            {
                failure = BadRequest(new { error = "Clue is not locked." });
            }
            else
            {
                session.LockedClues.Remove(request.ClueId);
                session.GoodTokens--;
                AddAction(session, new TeamActionEntry
                {
                    Actor = "good",
                    Action = "unlock",
                    ClueId = request.ClueId,
                    Room = request.Room,
                    Snippet = request.Snippet,
                    MemberId = member.MemberId,
                    DisplayName = member.DisplayName,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        if (failure != null)
        {
            return failure;
        }

        var response = SessionResponder.BuildTeam(session);
        response.PlayerId = member.MemberId;
        response.Role = member.Role;
        return Ok(response);
    }

    private string DetermineRole(SessionState session)
    {
        if (!session.TeamMembers.Any(m => m.Role == "villain"))
            return "villain";

        var villainCount = session.TeamMembers.Count(m => m.Role == "villain");
        if (villainCount >= 2)
            return "good";

        return _rng.Next(100) < 25 ? "villain" : "good";
    }

    private void AddAction(SessionState session, TeamActionEntry entry)
    {
        session.TeamActionLog.Insert(0, entry);
        if (session.TeamActionLog.Count > 8)
            session.TeamActionLog.RemoveAt(session.TeamActionLog.Count - 1);
    }
}
