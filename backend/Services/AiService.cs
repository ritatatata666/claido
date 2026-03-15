using System.Text;
using System.Text.Json;
using Claido.Models;

namespace Claido.Services;

public class AiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4o";

    public AiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = Environment.GetEnvironmentVariable("API_KEY")
            ?? config["API_KEY"]
            ?? throw new InvalidOperationException("API_KEY is not configured.");
    }

    private HttpRequestMessage BuildRequest(object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return req;
    }

    private static string ExtractContent(string raw)
    {
        var doc = JsonDocument.Parse(raw);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, int maxTokens = 4000)
    {
        var body = new
        {
            model = Model,
            max_tokens = maxTokens,
            temperature = 1.1,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  },
            }
        };

        var req = BuildRequest(body);
        var resp = await _http.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"OpenAI API error {resp.StatusCode}: {raw}");

        return ExtractContent(raw);
    }

    public async Task<string> ChatAsync(
        string systemPrompt,
        List<ConversationMessage> history,
        string userMessage,
        int maxTokens = 2000)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var m in history)
            messages.Add(new { role = m.Role, content = m.Content });

        messages.Add(new { role = "user", content = userMessage });

        var body = new
        {
            model = Model,
            max_tokens = maxTokens,
            messages
        };

        var req = BuildRequest(body);
        var resp = await _http.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"OpenAI API error {resp.StatusCode}: {raw}");

        return ExtractContent(raw);
    }

    public async Task<ClaudeGeneratedSession> GenerateSessionAsync()
    {
        var rng = new Random();
        var culpritId = 1001 + rng.Next(0, 8);
        var seed = rng.Next(10000, 99999);

        var prompt = $$"""
You are generating a mystery game session. Return ONLY valid JSON, no markdown, no explanation.
Seed: {{seed}} — use this to ensure a UNIQUE, creative scenario different from any previous run.

Generate a corporate breach mystery with these exact fields:
{
  "culpritId": {{culpritId}},
  "culpritName": "FirstName LastName",
  "culpritDepartment": "one of: Engineering, Security, Finance, HR, Executive",
  "culpritRole": "job title",
  "motive": "one vivid sentence — be creative, avoid clichés like 'greed' or 'revenge'",
  "incidentTime": "HH:MM between 00:00 and 03:00",
  "incidentDate": "2025-03-03",
  "employees": [
    { "id": 1001, "name": "...", "department": "...", "role": "...", "accessLevel": 3 },
    ... 8 employees total with ids 1001–1008, accessLevels 1–5 varied
  ],
  "badgeDiscrepancy": "one sentence describing what the physical badge log shows vs the digital access log",
  "vaultWord1": "single creative lowercase word with theme: time (NOT midnight, not dawn — pick something unusual)",
  "vaultWord2": "single creative lowercase word with theme: location (NOT vault, NOT server — pick something unusual)",
  "vaultWord3": "single creative lowercase word with theme: identity (NOT anonymous — pick something unusual)",
  "vaultWord4": "single creative lowercase word with theme: motive (NOT greed, NOT revenge — pick something unusual)"
}

IMPORTANT: The culprit must have employee id {{culpritId}}. All other employees are innocent.
Generate all 8 employees with realistic diverse names and roles. Vault words must all be DIFFERENT from each other.
STRICT OUTPUT RULES: Return a single valid JSON object only. No markdown, no code fences, no prose, no comments, no trailing commas.
""";

        var raw = await CompleteAsync("You are a mystery game content generator. Output only raw JSON.", prompt, 4000);

        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            var start = raw.IndexOf('\n') + 1;
            var end = raw.LastIndexOf("```");
            raw = raw[start..end].Trim();
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ClaudeGeneratedSession>(raw, options)
               ?? throw new Exception("Failed to deserialize session from OpenAI response.");
    }

    public async Task<string> GenerateRoomContentAsync(string roomName, SessionState session)
    {
        var culprit = session.Culprit;
        var contextJson = JsonSerializer.Serialize(new
        {
            culpritName = culprit.Name,
            culpritDepartment = culprit.Department,
            culpritRole = culprit.Role,
            culpritId = culprit.Id,
            motive = session.Motive,
            incidentTimestamp = session.IncidentTimestamp,
            badgeDiscrepancy = session.BadgeDiscrepancy,
            employees = session.Employees.Select(e => new { e.Id, e.Name, e.Department, e.Role, e.AccessLevel }),
            vaultWord1 = session.VaultWord1,
            vaultWord2 = session.VaultWord2,
            vaultWord3 = session.VaultWord3,
            vaultWord4 = session.VaultWord4
        });

        var prompt = roomName switch
        {
            "shell"  => BuildShellPrompt(contextJson, session),
            "mail"   => BuildMailPrompt(contextJson, session),
            "wiki"   => BuildWikiPrompt(contextJson, session),
            "search" => BuildSearchPrompt(contextJson, session),
            "onion"  => BuildOnionPrompt(contextJson, session),
            _ => throw new ArgumentException($"Unknown room: {roomName}")
        };

        var raw = await CompleteAsync(
            "You are a mystery game content generator. Output only raw JSON arrays/objects, no markdown.",
            prompt,
            4000);

        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            var start = raw.IndexOf('\n') + 1;
            var end = raw.LastIndexOf("```");
            raw = raw[start..end].Trim();
        }

        WriteRoomDebugDumpIfEnabled(roomName, session, raw);

        return RoomContentSanitizer.Normalize(roomName, raw, session);
    }

      private static void WriteRoomDebugDumpIfEnabled(string roomName, SessionState session, string raw)
      {
        if (!IsRoomDebugEnabled()) return;

        try
        {
          var safeRoom = new string(roomName.Where(char.IsLetterOrDigit).ToArray());
          if (string.IsNullOrWhiteSpace(safeRoom)) safeRoom = "room";
          var fileName = $"claido-{safeRoom}-{session.SessionId:N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.raw.json";
          var dumpPath = Path.Combine(Path.GetTempPath(), fileName);
          File.WriteAllText(dumpPath, raw);
          Console.WriteLine($"[RoomDebug] Saved raw {roomName} payload: {dumpPath}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[RoomDebug] Failed to write raw {roomName} payload: {ex.Message}");
        }
      }

      private static bool IsRoomDebugEnabled()
      {
        var value = Environment.GetEnvironmentVariable("CLAIDO_ROOM_DEBUG");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
          || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
          || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
      }

    private static string BuildShellPrompt(string ctx, SessionState s) => $$"""
Session context: {{ctx}}

Generate a fake corporate filesystem for a CTF game. Return JSON:
{
  "hostname": "novacorp-srv-01",
  "username": "analyst",
  "files": {
    "/home/analyst/.env": "VAULT_WORD=base64_of_{{s.VaultWord1}}\nDB_PASS=hunter2\nAPI_KEY=sk-fake-abc123",
    "/home/analyst/logs/access.log": "fake access log with 20 lines, formatted like [HH:MM:SS] LEVEL message, including one suspicious entry showing employee id {{s.Culprit.Id}} accessing server room at [{{ExtractLogTime(s.IncidentTimestamp)}}]",
    "/home/analyst/readme.txt": "Welcome to NovaCorp internal analyst workstation.",
    "/home/analyst/notes.txt": "Reminder: badge system was offline between 01:00-03:00 on incident date.",
    "/etc/passwd": "root:x:0:0:root:/root:/bin/bash\nanalyst:x:1000:1000::/home/analyst:/bin/bash",
    "/var/log/syslog": "fake syslog with 10 lines including a failed sudo attempt"
  },
  "dirs": ["/home/analyst", "/home/analyst/logs", "/etc", "/var/log", "/tmp"]
}

IMPORTANT: In /home/analyst/.env, the VAULT_WORD value must be the base64 encoding of "{{s.VaultWord1}}" (just that word, no newline).
IMPORTANT: Every line in /home/analyst/logs/access.log must use the timestamp format [HH:MM:SS], not ISO 8601.
STRICT OUTPUT RULES: Return a single valid JSON object only. No markdown, no code fences, no prose, no comments, no trailing commas.
""";

    private static string ExtractLogTime(string incidentTimestamp)
    {
        if (DateTime.TryParse(incidentTimestamp, out var parsed))
        {
            return parsed.ToString("HH:mm:ss");
        }

        return "01:17:00";
    }

    private static string BuildMailPrompt(string ctx, SessionState s) => $$"""
Session context: {{ctx}}

Generate 12 fake corporate emails for a CTF mystery game. The culprit is {{s.Culprit.Name}} (id {{s.Culprit.Id}}).
One email must contain a subtle clue about the vault word "{{s.VaultWord2}}" hidden in the text.
Return a JSON array:
[
  {
    "id": "msg-001",
    "from": "name@novacorp.com",
    "to": "analyst@novacorp.com",
    "subject": "...",
    "date": "2025-03-02T...",
    "body": "...",
    "isRead": false,
    "folder": "inbox"
  }
]
Include emails spanning several days before incident. At least 2 emails show tension or urgency.
One email from an unknown external address hints at a secret meeting the night of the incident.
STRICT OUTPUT RULES: Return a raw JSON array only (top-level must be `[...]`). No markdown, no code fences, no prose, no comments, no trailing commas.
""";

    private static string BuildWikiPrompt(string ctx, SessionState s) => $$"""
Session context: {{ctx}}

Generate 6 fake corporate wiki pages for a CTF mystery game. The culprit is {{s.Culprit.Name}}.
Exactly ONE page must have hasRedacted=true with a redactedSection that is the ROT13 encoding of a sentence containing the vault word "{{s.VaultWord3}}".
All other pages have hasRedacted=false and no redactedSection.
Return a JSON array:
[
  {
    "id": "page-001",
    "title": "...",
    "category": "...",
    "lastModified": "2025-03-01",
    "author": "...",
    "content": "markdown content of page",
    "hasRedacted": false
  },
  {
    "id": "page-002",
    "title": "...",
    "category": "Security",
    "lastModified": "2025-03-01",
    "author": "...",
    "content": "page content without the redacted part",
    "hasRedacted": true,
    "redactedSection": "ROT13 of a sentence containing the word {{s.VaultWord3}}"
  }
]
Include pages about: employee handbook, security protocols, incident response, org chart, project Nova, server room access.
STRICT OUTPUT RULES: Return a raw JSON array only (top-level must be `[...]`). No markdown, no code fences, no prose, no comments, no trailing commas.
""";

    private static string BuildSearchPrompt(string ctx, SessionState s) => $$"""
Session context: {{ctx}}

Generate exactly 50 fake log entries for a Kibana-style log search interface. The culprit is {{s.Culprit.Name}} (id {{s.Culprit.Id}}).
Use employee id {{ResolveWhistleblowerUserId(s)}} as the confidential witness. One log entry must have user="{{ResolveWhistleblowerUserId(s)}}" with level ERROR, contain vault word "{{s.VaultWord4}}", and implicate the culprit. Do not use the literal string "whistleblower" in the user field.
Return a JSON array:
[
  {
    "id": "log-001",
    "timestamp": "2025-03-03T00:...",
    "level": "INFO|WARN|ERROR|DEBUG",
    "service": "auth|api|db|badge|mail|vault",
    "user": "employee id",
    "message": "...",
    "ip": "192.168.x.x"
  }
]
Mix of log levels. Include several suspicious entries around {{s.IncidentTimestamp}}. The ERROR from employee {{ResolveWhistleblowerUserId(s)}} should be around index 30-40.
STRICT OUTPUT RULES: Return a raw JSON array only (top-level must be `[...]`). No markdown, no code fences, no prose, no comments, no trailing commas.
""";

    private static string ResolveWhistleblowerUserId(SessionState session)
    {
        return session.Employees.FirstOrDefault(employee => employee.Id != session.Culprit.Id)?.Id.ToString()
            ?? session.Employees.First().Id.ToString();
    }

    private static string BuildOnionPrompt(string ctx, SessionState s) => $$"""
Session context: {{ctx}}

Generate dark web forum content for a CTF mystery game. The culprit is {{s.Culprit.Name}}.
Return JSON:
{
  "forumPosts": [
    {
      "id": "post-001",
      "handle": "...",
      "timestamp": "...",
      "category": "...",
      "title": "...",
      "body": "...",
      "replies": 0
    }
  ],
  "marketListings": [
    {
      "id": "listing-001",
      "seller": "...",
      "title": "...",
      "price": "0.003 BTC",
      "description": "...",
      "category": "data|access|credentials|services"
    }
  ]
}
Generate 8 forum posts and 5 marketplace listings. One listing must be for stolen NovaCorp credentials hinting at the culprit's department.
The forum posts should build an atmosphere of corporate espionage. One post references the incident date.
STRICT OUTPUT RULES: Return a single valid JSON object only. No markdown, no code fences, no prose, no comments, no trailing commas.
""";
}
