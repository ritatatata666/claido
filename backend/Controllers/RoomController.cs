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

    public RoomController(
        AiService claude,
        ConcurrentDictionary<Guid, SessionState> sessions)
    {
        _claude = claude;
        _sessions = sessions;
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
}
