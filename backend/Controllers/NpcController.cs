using System.Collections.Concurrent;
using Claido.Models;
using Claido.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claido.Controllers;

[ApiController]
[Route("api/session/{sessionId}/npc")]
[Authorize]
public class NpcController : ControllerBase
{
    private readonly AiService _claude;
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions;

    public NpcController(
        AiService claude,
        ConcurrentDictionary<Guid, SessionState> sessions)
    {
        _claude = claude;
        _sessions = sessions;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(Guid sessionId, [FromBody] NpcChatRequest req)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Session not found." });

        try
        {
            var systemPrompt = NpcService.GetSystemPrompt(req.NpcId, session);
            var reply = await _claude.ChatAsync(systemPrompt, req.ConversationHistory, req.Message, 2000);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
