using System.Collections.Concurrent;
using System.Security.Claims;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session/{sessionId}/room/{roomName}")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly AiService _claude;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;
    private readonly UserStore _users;
    private readonly ConcurrentDictionary<string, LeaderboardEntry> _leaderboard;

    public RoomController(
        AiService claude,
        ConcurrentDictionary<Guid, SessionState> sessions,
        UserStore users,
        ConcurrentDictionary<string, LeaderboardEntry> leaderboard)
    {
        _claude = claude;
        _sessions = sessions;
        _users = users;
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
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { error = "Not authenticated." });

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

        if (correct && roomName == "vault" && !session.HistoryRecorded)
        {
            var elapsed = req.ElapsedSeconds.GetValueOrDefault((int)(DateTime.UtcNow - session.StartedAtUtc).TotalSeconds);
            var points = req.Points.GetValueOrDefault(Math.Max(0, 5000 - elapsed * 4));
            _users.AddHistory(userId, new GameHistoryEntry
            {
                SessionId = session.SessionId,
                StartedAtUtc = session.StartedAtUtc,
                CompletedAtUtc = DateTime.UtcNow,
                ElapsedSeconds = Math.Max(0, elapsed),
                Points = Math.Max(0, points),
                WrongAnswers = Math.Max(0, req.WrongAnswers.GetValueOrDefault(0)),
                TimePenaltySeconds = Math.Max(0, req.TimePenaltySeconds.GetValueOrDefault(0)),
                TeamMode = session.TeamMode,
                CaseFile = $"CASE {session.SessionId.ToString()[..8].ToUpperInvariant()}",
                CulpritName = session.Culprit?.Name ?? "",
                Questions = BuildQuestionReview(session),
            });
            session.HistoryRecorded = true;
        }
        object? leaderboard = null;

        if (correct && roomName == "vault")
        {
            leaderboard = RecordSolve(session, req.MemberId);
        }

        return Ok(new { correct, hint });
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

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private static List<QuestionReviewEntry> BuildQuestionReview(SessionState session)
    {
        return new List<QuestionReviewEntry>
        {
            new()
            {
                QuestionId = "shell-1",
                Room = "NovaShell",
                Prompt = "What is the Shell room keyword?",
                Solution = session.VaultWord1
            },
            new()
            {
                QuestionId = "mail-1",
                Room = "NovaMail",
                Prompt = "What is the Mail room keyword?",
                Solution = session.VaultWord2
            },
            new()
            {
                QuestionId = "wiki-1",
                Room = "NovaWiki",
                Prompt = "What is the Wiki room keyword?",
                Solution = session.VaultWord3
            },
            new()
            {
                QuestionId = "search-1",
                Room = "NovaSearch",
                Prompt = "What is the Search room keyword?",
                Solution = session.VaultWord4
            },
            new()
            {
                QuestionId = "vault-1",
                Room = "Vault",
                Prompt = "What is the final four-word vault passphrase?",
                Solution = session.VaultCode
            }
        };
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

        return "Player";
    }
}
