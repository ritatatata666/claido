using System;
using System.Collections.Concurrent;
using System.Linq;
using Claido.Models;

namespace Claido.Services;

public class SessionCreator
{
    private readonly AiService _claude;
    private readonly ConcurrentDictionary<string, Guid> _joinCodes;
    private readonly Random _rng = new();

    public SessionCreator(AiService claude, ConcurrentDictionary<string, Guid> joinCodes)
    {
        _claude = claude;
        _joinCodes = joinCodes;
    }

    public async Task<SessionState> CreateSessionAsync()
    {
        var generated = await _claude.GenerateSessionAsync();

        var culprit = generated.Employees.FirstOrDefault(e =>
                          e.Name.Equals(generated.CulpritName, StringComparison.OrdinalIgnoreCase))
                      ?? generated.Employees.FirstOrDefault()
                      ?? new Employee { Name = generated.CulpritName, Department = generated.CulpritDepartment, Role = generated.CulpritRole };

        culprit.Name = generated.CulpritName;
        culprit.Department = generated.CulpritDepartment;
        culprit.Role = generated.CulpritRole;

        var session = new SessionState
        {
            Culprit = culprit,
            Employees = generated.Employees,
            IncidentTimestamp = $"{generated.IncidentDate}T{generated.IncidentTime}:00",
            BadgeDiscrepancy = generated.BadgeDiscrepancy,
            Motive = generated.Motive,
            VaultWord1 = generated.VaultWord1.ToLower(),
            VaultWord2 = generated.VaultWord2.ToLower(),
            VaultWord3 = generated.VaultWord3.ToLower(),
            VaultWord4 = generated.VaultWord4.ToLower()
        };

        session.JoinCode = GenerateJoinCode(session.SessionId);
        return session;
    }

    private string GenerateJoinCode(Guid sessionId)
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6)
                .Select(_ => alphabet[_rng.Next(alphabet.Length)])
                .ToArray());

            if (_joinCodes.TryAdd(code, sessionId))
            {
                return code;
            }
        }
    }
}
