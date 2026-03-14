namespace Claido.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Role { get; set; } = "";
    public int AccessLevel { get; set; }
}

public class SessionState
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Employee Culprit { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public string IncidentTimestamp { get; set; } = "";
    public string BadgeDiscrepancy { get; set; } = "";
    public string Motive { get; set; } = "";
    public string VaultWord1 { get; set; } = "";
    public string VaultWord2 { get; set; } = "";
    public string VaultWord3 { get; set; } = "";
    public string VaultWord4 { get; set; } = "";
    public string VaultCode => $"{VaultWord1} {VaultWord2} {VaultWord3} {VaultWord4}";
    public string JoinCode { get; set; } = "";
    public string TeamMode { get; set; } = "standard";
    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<TeamActionEntry> TeamActionLog { get; set; } = new();
    public HashSet<string> LockedClues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int VillainTokens { get; set; } = 3;
    public int GoodTokens { get; set; } = 2;
}

public class ClaudeGeneratedSession
{
    public string CulpritName { get; set; } = "";
    public string CulpritDepartment { get; set; } = "";
    public string CulpritRole { get; set; } = "";
    public int CulpritId { get; set; } = 1001;
    public string Motive { get; set; } = "";
    public string IncidentTime { get; set; } = "";
    public string IncidentDate { get; set; } = "";
    public List<Employee> Employees { get; set; } = new();
    public string BadgeDiscrepancy { get; set; } = "";
    public string VaultWord1 { get; set; } = "";
    public string VaultWord2 { get; set; } = "";
    public string VaultWord3 { get; set; } = "";
    public string VaultWord4 { get; set; } = "";
}

public class NpcChatRequest
{
    public string NpcId { get; set; } = "";
    public string Message { get; set; } = "";
    public List<ConversationMessage> ConversationHistory { get; set; } = new();
}

public class ConversationMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class ValidateRequest
{
    public string Answer { get; set; } = "";
}

public class TeamMember
{
    public Guid MemberId { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "good";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class TeamActionEntry
{
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string ClueId { get; set; } = "";
    public string Room { get; set; } = "";
    public string Snippet { get; set; } = "";
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class JoinTeamRequest
{
    public string JoinCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class CreateTeamRoomRequest
{
    public string DisplayName { get; set; } = "";
}

public class TeamClueRequest
{
    public Guid MemberId { get; set; }
    public string ClueId { get; set; } = "";
    public string Room { get; set; } = "";
    public string Snippet { get; set; } = "";
}

public class SessionBaseResponse
{
    public Guid SessionId { get; set; }
    public object Culprit { get; set; } = new();
    public IEnumerable<object> Employees { get; set; } = Array.Empty<object>();
    public string IncidentTimestamp { get; set; } = "";
    public string BadgeDiscrepancy { get; set; } = "";
    public string Motive { get; set; } = "";
    public string VaultWord1 { get; set; } = "";
    public string VaultWord2 { get; set; } = "";
    public string VaultWord3 { get; set; } = "";
    public string VaultWord4 { get; set; } = "";
    public string VaultCode { get; set; } = "";
    public string JoinCode { get; set; } = "";
    public int VillainTokens { get; set; }
    public int GoodTokens { get; set; }
    public string TeamMode { get; set; } = "standard";
}

public class SessionTeamResponse : SessionBaseResponse
{
    public SessionTeamResponse() { }

    public SessionTeamResponse(SessionBaseResponse source)
    {
        SessionId = source.SessionId;
        Culprit = source.Culprit;
        Employees = source.Employees;
        IncidentTimestamp = source.IncidentTimestamp;
        BadgeDiscrepancy = source.BadgeDiscrepancy;
        Motive = source.Motive;
        VaultWord1 = source.VaultWord1;
        VaultWord2 = source.VaultWord2;
        VaultWord3 = source.VaultWord3;
        VaultWord4 = source.VaultWord4;
        VaultCode = source.VaultCode;
        JoinCode = source.JoinCode;
        VillainTokens = source.VillainTokens;
        GoodTokens = source.GoodTokens;
        TeamMode = source.TeamMode;
    }

    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<TeamActionEntry> TeamActionLog { get; set; } = new();
    public List<string> LockedClues { get; set; } = new();
    public Guid PlayerId { get; set; }
    public string Role { get; set; } = "";
}
