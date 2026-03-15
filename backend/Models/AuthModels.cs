namespace Claido.Models;

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UserInfoResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public class GameHistoryEntry
{
    public Guid SessionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public int ElapsedSeconds { get; set; }
    public int Points { get; set; }
    public int WrongAnswers { get; set; }
    public int TimePenaltySeconds { get; set; }
    public string TeamMode { get; set; } = "standard";
    public string CaseFile { get; set; } = "";
    public string CulpritName { get; set; } = "";
    public List<QuestionReviewEntry> Questions { get; set; } = new();
}

public class QuestionReviewEntry
{
    public string QuestionId { get; set; } = "";
    public string Room { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Solution { get; set; } = "";
}

public class UserRecord
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string PasswordHashBase64 { get; set; } = "";
    public string PasswordSaltBase64 { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<GameHistoryEntry> History { get; set; } = new();
}

public class UserStoreFile
{
    public List<UserRecord> Users { get; set; } = new();
}
