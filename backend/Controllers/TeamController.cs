using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session")]
[Authorize]
public class TeamController : ControllerBase
{
    private const int MaxTeamMembers = 3;
    private readonly SessionCreator _sessionCreator;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;
    private readonly ConcurrentDictionary<string, Guid> _joinCodes;

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
        if (!TryGetUserId(out _))
            return Unauthorized(new { error = "Not authenticated." });

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

        TeamMember? member = null;
        IActionResult? failure = null;
        lock (session)
        {
            if (session.TeamMembers.Count >= MaxTeamMembers)
            {
                failure = BadRequest(new { error = $"Team is full (max {MaxTeamMembers} players)." });
            }
            else
            {
            member = new TeamMember
            {
                DisplayName = displayName,
                Role = ResolveRole(request.PreferredRole, session),
                JoinedAt = DateTime.UtcNow
            };
            session.TeamMembers.Add(member);
            session.TeamMode = "team";
            }
        }
        if (failure != null)
            return failure;
        if (member == null)
            return BadRequest(new { error = "Failed to join team." });

        var response = SessionResponder.BuildTeam(session);
        response.PlayerId = member.MemberId;
        response.Role = member.Role;
        return Ok(response);
    }

    [HttpPost("team/create")]
    public async Task<IActionResult> CreateTeamRoom([FromBody] CreateTeamRoomRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { error = "Not authenticated." });

        try
        {
            var session = await _sessionCreator.CreateSessionAsync();
            session.OwnerUserId = userId;
            session.StartedAtUtc = DateTime.UtcNow;
            session.TeamMode = "team";
            _sessions[session.SessionId] = session;

            var displayName = string.IsNullOrWhiteSpace(request?.DisplayName)
                ? "Host"
                : request.DisplayName.Trim();

            TeamMember member;

            lock (session)
            {
                member = new TeamMember
                {
                    DisplayName = displayName,
                    Role = ResolveRole(request?.PreferredRole, session),
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
            else if (session.InvestigatorFoundClues.Contains(request.ClueId))
            {
                failure = BadRequest(new { error = "Cannot sabotage a clue already found by an investigator." });
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
                session.InvestigatorFoundClues.Add(request.ClueId);
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

    [HttpPost("{sessionId}/team/discover")]
    public IActionResult Discover(Guid sessionId, [FromBody] TeamClueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClueId))
            return BadRequest(new { error = "Clue id is required." });

        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        var member = session.TeamMembers.FirstOrDefault(m => m.MemberId == request.MemberId);
        if (member == null || member.Role != "good")
            return BadRequest(new { error = "Only investigators can mark clues as found." });

        lock (session)
        {
            session.InvestigatorFoundClues.Add(request.ClueId);
        }

        var response = SessionResponder.BuildTeam(session);
        response.PlayerId = member.MemberId;
        response.Role = member.Role;
        return Ok(response);
    }

    private string ResolveRole(string? preferredRole, SessionState session)
    {
        var normalized = preferredRole?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "villain" => "villain",
            "investigator" => "good",
            "good" => "good",
            _ => "good"
        };
    }

    private void AddAction(SessionState session, TeamActionEntry entry)
    {
        session.TeamActionLog.Insert(0, entry);
        if (session.TeamActionLog.Count > 8)
            session.TeamActionLog.RemoveAt(session.TeamActionLog.Count - 1);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
