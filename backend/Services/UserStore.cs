using System.Security.Cryptography;
using System.Text.Json;
using Claido.Models;

namespace Claido.Services;

public class UserStore
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly string _filePath;
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "users.json");
        EnsureInitializedFile();
    }

    public (bool Success, string Error) Register(string username, string password)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (!ValidateUsername(normalizedUsername, out var usernameError))
        {
            return (false, usernameError);
        }

        if (!ValidatePassword(password, out var passwordError))
        {
            return (false, passwordError);
        }

        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            if (file.Users.Any(u => u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Username already exists.");
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = HashPassword(password, salt);

            file.Users.Add(new UserRecord
            {
                UserId = Guid.NewGuid(),
                Username = normalizedUsername,
                PasswordSaltBase64 = Convert.ToBase64String(salt),
                PasswordHashBase64 = Convert.ToBase64String(hash),
                CreatedAtUtc = DateTime.UtcNow
            });

            SaveFileUnsafe(file);
            return (true, "");
        }
    }

    public UserRecord? Authenticate(string username, string password)
    {
        var normalizedUsername = NormalizeUsername(username);
        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            var user = file.Users.FirstOrDefault(u =>
                u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                return null;
            }

            var salt = Convert.FromBase64String(user.PasswordSaltBase64);
            var expected = Convert.FromBase64String(user.PasswordHashBase64);
            var actual = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? user : null;
        }
    }

    public UserRecord? FindById(Guid userId)
    {
        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            return file.Users.FirstOrDefault(u => u.UserId == userId);
        }
    }

    public List<GameHistoryEntry> GetHistory(Guid userId)
    {
        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            var user = file.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return new List<GameHistoryEntry>();
            return user.History
                .OrderByDescending(h => h.CompletedAtUtc)
                .Take(25)
                .ToList();
        }
    }

    public GameHistoryEntry? GetHistoryCase(Guid userId, Guid sessionId)
    {
        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            var user = file.Users.FirstOrDefault(u => u.UserId == userId);
            return user?.History.FirstOrDefault(h => h.SessionId == sessionId);
        }
    }

    public void AddHistory(Guid userId, GameHistoryEntry entry)
    {
        lock (_syncRoot)
        {
            var file = LoadFileUnsafe();
            var user = file.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return;
            user.History.Insert(0, entry);
            if (user.History.Count > 100)
            {
                user.History = user.History.Take(100).ToList();
            }
            SaveFileUnsafe(file);
        }
    }

    private void EnsureInitializedFile()
    {
        lock (_syncRoot)
        {
            if (File.Exists(_filePath)) return;
            SaveFileUnsafe(new UserStoreFile());
        }
    }

    private UserStoreFile LoadFileUnsafe()
    {
        var raw = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<UserStoreFile>(raw, _jsonOptions) ?? new UserStoreFile();
    }

    private void SaveFileUnsafe(UserStoreFile file)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(file, _jsonOptions));
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }

    private static string NormalizeUsername(string username)
    {
        return (username ?? "").Trim();
    }

    private static bool ValidateUsername(string username, out string error)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            error = "Username is required.";
            return false;
        }

        if (username.Length < 3 || username.Length > 32)
        {
            error = "Username must be 3-32 characters.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool ValidatePassword(string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Password is required.";
            return false;
        }

        if (password.Length < 8)
        {
            error = "Password must be at least 8 characters.";
            return false;
        }

        error = "";
        return true;
    }
}
