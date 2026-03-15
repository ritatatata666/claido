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
    private const int MaxWrongAttemptsPerRoom = 5;
    private static readonly HashSet<string> RateLimitedRooms = new(StringComparer.OrdinalIgnoreCase)
    {
        "shell",
        "database",
        "mail",
        "wiki",
        "search",
        "onion",
        "vault"
    };

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

        roomName = NormalizeRoomName(roomName);
        var answer = (req.Answer ?? string.Empty).Trim().ToLowerInvariant();

        if (!RateLimitedRooms.Contains(roomName))
            return BadRequest(new { error = $"Room '{roomName}' does not support answer validation." });

        var currentAttemptState = GetAttemptState(session, roomName);
        if (currentAttemptState.locked)
        {
            return Ok(new
            {
                correct = false,
                locked = true,
                hint = BuildLockedHint(roomName),
                leaderboard = (object?)null,
                wrongAttempts = currentAttemptState.wrongAttempts,
                attemptsRemaining = currentAttemptState.attemptsRemaining,
                penaltySecondsAdded = 0,
                totalPenaltySeconds = currentAttemptState.totalPenaltySeconds,
                maxAttempts = MaxWrongAttemptsPerRoom,
            });
        }

        bool correct = roomName switch
        {
            "shell" => answer == session.VaultWord1,
            "mail" => answer == session.VaultWord2,
            "wiki" => answer == session.VaultWord3,
            "search" => answer == session.VaultWord4,
            "vault" => ValidateVaultAnswer(answer, session),
            _ => false
        };

        object? leaderboard = null;
        if (correct && roomName == "vault")
        {
            leaderboard = RecordSolve(session, req.MemberId);
        }

        if (correct)
        {
            return Ok(new
            {
                correct = true,
                locked = false,
                hint = (string?)null,
                leaderboard,
                wrongAttempts = currentAttemptState.wrongAttempts,
                attemptsRemaining = currentAttemptState.attemptsRemaining,
                penaltySecondsAdded = 0,
                totalPenaltySeconds = currentAttemptState.totalPenaltySeconds,
                maxAttempts = MaxWrongAttemptsPerRoom,
            });
        }

        var failedAttemptState = ApplyWrongAttempt(session, roomName);
        var hint = failedAttemptState.locked
            ? BuildLockedHint(roomName)
            : roomName switch
            {
                "vault" => "The passphrase is four words separated by spaces. Look in your clues.",
                _ => "Not quite. Keep searching the room for clues."
            };

        return Ok(new
        {
            correct = false,
            locked = failedAttemptState.locked,
            hint,
            leaderboard = (object?)null,
            wrongAttempts = failedAttemptState.wrongAttempts,
            attemptsRemaining = failedAttemptState.attemptsRemaining,
            penaltySecondsAdded = failedAttemptState.penaltySecondsAdded,
            totalPenaltySeconds = failedAttemptState.totalPenaltySeconds,
            maxAttempts = MaxWrongAttemptsPerRoom,
        });
    }

    [HttpPost("wrong-attempt")]
    public IActionResult RegisterWrongAttempt(Guid sessionId, string roomName)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        roomName = NormalizeRoomName(roomName);
        if (!RateLimitedRooms.Contains(roomName))
            return BadRequest(new { error = $"Room '{roomName}' does not support attempt tracking." });

        var failedAttemptState = ApplyWrongAttempt(session, roomName);

        return Ok(new
        {
            roomName,
            locked = failedAttemptState.locked,
            wrongAttempts = failedAttemptState.wrongAttempts,
            attemptsRemaining = failedAttemptState.attemptsRemaining,
            penaltySecondsAdded = failedAttemptState.penaltySecondsAdded,
            totalPenaltySeconds = failedAttemptState.totalPenaltySeconds,
            maxAttempts = MaxWrongAttemptsPerRoom,
            hint = failedAttemptState.locked ? BuildLockedHint(roomName) : (string?)null,
        });
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
                var elapsedSeconds = (int)Math.Round((completedAt - session.StartedAtUtc).TotalSeconds);
                var solveSeconds = Math.Max(1, elapsedSeconds + Math.Max(0, session.PenaltySecondsTotal));

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

    private static string NormalizeRoomName(string roomName)
    {
        return (roomName ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string BuildLockedHint(string roomName)
    {
        return $"{GetRoomDisplayName(roomName)} locked: maximum failed attempts reached.";
    }

    private static string GetRoomDisplayName(string roomName)
    {
        return roomName switch
        {
            "shell" => "NovaShell",
            "database" => "NovaCrime DB",
            "mail" => "NovaMail",
            "wiki" => "NovaWiki",
            "search" => "NovaSearch",
            "onion" => "Onion room",
            "vault" => "Vault",
            _ => roomName,
        };
    }

    private static (int wrongAttempts, int attemptsRemaining, bool locked, int totalPenaltySeconds) GetAttemptState(SessionState session, string roomName)
    {
        lock (session)
        {
            session.WrongAttemptsByRoom.TryGetValue(roomName, out var wrongAttempts);
            wrongAttempts = Math.Max(0, wrongAttempts);
            return (
                wrongAttempts,
                Math.Max(0, MaxWrongAttemptsPerRoom - wrongAttempts),
                wrongAttempts >= MaxWrongAttemptsPerRoom,
                Math.Max(0, session.PenaltySecondsTotal));
        }
    }

    private static (int wrongAttempts, int attemptsRemaining, bool locked, int penaltySecondsAdded, int totalPenaltySeconds) ApplyWrongAttempt(SessionState session, string roomName)
    {
        lock (session)
        {
            session.WrongAttemptsByRoom.TryGetValue(roomName, out var currentAttempts);
            currentAttempts = Math.Max(0, currentAttempts);

            if (currentAttempts >= MaxWrongAttemptsPerRoom)
            {
                return (
                    currentAttempts,
                    0,
                    true,
                    0,
                    Math.Max(0, session.PenaltySecondsTotal));
            }

            var updatedAttempts = currentAttempts + 1;
            var penaltySecondsAdded = updatedAttempts;
            session.WrongAttemptsByRoom[roomName] = updatedAttempts;
            session.PenaltySecondsTotal += penaltySecondsAdded;

            return (
                updatedAttempts,
                Math.Max(0, MaxWrongAttemptsPerRoom - updatedAttempts),
                updatedAttempts >= MaxWrongAttemptsPerRoom,
                penaltySecondsAdded,
                Math.Max(0, session.PenaltySecondsTotal));
        }
    }
}
