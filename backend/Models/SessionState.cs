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
