using Claido.Models;
using System.Linq;

namespace Claido.Services;

public static class SessionResponder
{
    public static SessionBaseResponse Build(SessionState session)
    {
        return new SessionBaseResponse
        {
            SessionId = session.SessionId,
            Culprit = new
            {
                session.Culprit.Id,
                session.Culprit.Name,
                session.Culprit.Department
            },
            Employees = session.Employees.Select(e => new
            {
                e.Id,
                e.Name,
                e.Department,
                e.Role,
                e.AccessLevel
            }),
            IncidentTimestamp = session.IncidentTimestamp,
            BadgeDiscrepancy = session.BadgeDiscrepancy,
            Motive = session.Motive,
            VaultWord1 = session.VaultWord1,
            VaultWord2 = session.VaultWord2,
            VaultWord3 = session.VaultWord3,
            VaultWord4 = session.VaultWord4,
            VaultCode = session.VaultCode,
            JoinCode = session.JoinCode,
            VillainTokens = session.VillainTokens,
            GoodTokens = session.GoodTokens,
            TeamMode = session.TeamMode
        };
    }

    public static SessionTeamResponse BuildTeam(SessionState session)
    {
        var baseResponse = Build(session);
        var teamResponse = new SessionTeamResponse(baseResponse)
        {
            TeamMembers = session.TeamMembers.ToList(),
            TeamActionLog = session.TeamActionLog.ToList(),
            LockedClues = session.LockedClues.ToList()
        };
        return teamResponse;
    }
}
