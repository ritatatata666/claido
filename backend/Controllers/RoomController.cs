using System.Collections.Concurrent;
using System.Text.Json;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session/{sessionId}/room/{roomName}")]
public class RoomController : ControllerBase
{
    private readonly AiService _claude;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;
    private readonly ConcurrentDictionary<string, LeaderboardEntry> _leaderboard;

    public RoomController(
        AiService claude,
        ConcurrentDictionary<Guid, SessionState> sessions,
        ConcurrentDictionary<string, LeaderboardEntry> leaderboard)
    {
        _claude = claude;
        _sessions = sessions;
        _leaderboard = leaderboard;
    }

    [HttpPost("enter")]
    public async Task<IActionResult> EnterRoom(Guid sessionId, string roomName)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        roomName = roomName.ToLower();

        try
        {
            if (roomName == "database")
            {
                var dbBase64 = DatabaseService.GenerateSqliteBase64(session);
                return Ok(new { dbBase64 });
            }

            if (roomName == "vault")
            {
                return Ok(new
                {
                    message = "Enter the four-word passphrase to unlock the vault.",
                    hint = "Each word was hidden in one of the previous rooms."
                });
            }

            var contentJson = await _claude.GenerateRoomContentAsync(roomName, session);

            return Content(contentJson, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("validate")]
    public IActionResult Validate(Guid sessionId, string roomName, [FromBody] ValidateRequest req)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        roomName = roomName.ToLower();
        var answer = req.Answer.Trim().ToLower();

        bool correct = roomName switch
        {
            "shell" => answer == session.VaultWord1,
            "mail" => answer == session.VaultWord2,
            "wiki" => answer == session.VaultWord3,
            "search" => answer == session.VaultWord4,
            "vault" => ValidateVaultAnswer(answer, session),
            _ => false
        };

        string? hint = correct ? null : roomName switch
        {
            "vault" => "The passphrase is four words separated by spaces. Look in your clues.",
            _ => "Not quite. Keep searching the room for clues."
        };

        object? leaderboard = null;
        if (correct && roomName == "vault")
        {
            leaderboard = RecordSolve(session, req.MemberId);
        }

        return Ok(new { correct, hint, leaderboard });
    }

    private static bool ValidateVaultAnswer(string answer, SessionState session)
    {
        var parts = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        return parts[0] == session.VaultWord1
            && parts[1] == session.VaultWord2
            && parts[2] == session.VaultWord3
            && parts[3] == session.VaultWord4;
    }

    private IEnumerable<object> RecordSolve(SessionState session, Guid? memberId)
    {
        lock (session)
        {
            if (!session.CompletedAtUtc.HasValue)
            {
                var completedAt = DateTime.UtcNow;
                session.CompletedAtUtc = completedAt;

                var displayName = ResolveDisplayName(session, memberId);
                var solveSeconds = Math.Max(1, (int)Math.Round((completedAt - session.StartedAtUtc).TotalSeconds));

                _leaderboard.AddOrUpdate(
                    displayName,
                    _ => new LeaderboardEntry
                    {
                        DisplayName = displayName,
                        SolveSeconds = solveSeconds,
                        CompletedAtUtc = completedAt,
                    },
                    (_, existing) => existing.SolveSeconds <= solveSeconds
                        ? existing
                        : new LeaderboardEntry
                        {
                            DisplayName = displayName,
                            SolveSeconds = solveSeconds,
                            CompletedAtUtc = completedAt,
                        });
            }
        }

        return _leaderboard.Values
            .OrderBy(entry => entry.SolveSeconds)
            .ThenBy(entry => entry.CompletedAtUtc)
            .Take(5)
            .Select(entry => new
            {
                displayName = entry.DisplayName,
                solveSeconds = entry.SolveSeconds,
                completedAtUtc = entry.CompletedAtUtc,
            });
    }

    private static string ResolveDisplayName(SessionState session, Guid? memberId)
    {
        if (memberId.HasValue)
        {
            var member = session.TeamMembers.FirstOrDefault(item => item.MemberId == memberId.Value);
            if (member != null && !string.IsNullOrWhiteSpace(member.DisplayName))
                return member.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(session.InvestigatorName))
            return session.InvestigatorName.Trim();

        return "Investigator";
    }
}
