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

    public RoomController(
        AiService claude,
        ConcurrentDictionary<Guid, SessionState> sessions,
        UserStore users)
    {
        _claude = claude;
        _sessions = sessions;
        _users = users;
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
                TeamMode = session.TeamMode
            });
            session.HistoryRecorded = true;
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
}
