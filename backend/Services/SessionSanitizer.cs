using System.Globalization;
using System.Text.RegularExpressions;
using Claido.Models;

namespace Claido.Services;

public static class SessionSanitizer
{
    private static readonly string[] FallbackVaultWords =
    {
        "afterglow",
        "mezzanine",
        "alias",
        "leverage",
    };

    private static readonly (int Id, string Name, string Department, string Role, int AccessLevel)[] FallbackEmployees =
    {
        (1001, "Nina Park", "Engineering", "Platform Engineer", 3),
        (1002, "Owen Brooks", "Security", "Security Analyst", 4),
        (1003, "Priya Singh", "Finance", "Finance Manager", 3),
        (1004, "Marcus Bell", "HR", "HR Business Partner", 2),
        (1005, "Elena Cruz", "Executive", "Chief of Staff", 5),
        (1006, "Jonah Lee", "Engineering", "SRE", 4),
        (1007, "Hannah Cole", "Security", "Identity Engineer", 3),
        (1008, "Daniel Wu", "Finance", "Controller", 4),
    };

    public static ClaudeGeneratedSession Normalize(ClaudeGeneratedSession? generated)
    {
        generated ??= new ClaudeGeneratedSession();

        var employees = NormalizeEmployees(generated.Employees);
        var culpritId = ResolveCulpritId(generated, employees);
        var culprit = employees.FirstOrDefault(employee => employee.Id == culpritId);
        if (culprit is null)
        {
            culprit = BuildFallbackEmployee(culpritId);
            employees.Add(culprit);
        }

        culprit.Name = ValueOrFallback(generated.CulpritName, culprit.Name);
        culprit.Department = ValueOrFallback(generated.CulpritDepartment, culprit.Department);
        culprit.Role = ValueOrFallback(generated.CulpritRole, culprit.Role);
        culprit.AccessLevel = culprit.AccessLevel <= 0 ? 4 : culprit.AccessLevel;

        EnsureAtLeastOneWitness(employees, culprit.Id);

        var vaultWords = NormalizeVaultWords(generated);

        return new ClaudeGeneratedSession
        {
            CulpritId = culprit.Id,
            CulpritName = culprit.Name,
            CulpritDepartment = culprit.Department,
            CulpritRole = culprit.Role,
            Employees = employees.OrderBy(employee => employee.Id).ToList(),
            Motive = ValueOrFallback(generated.Motive, "They were trying to bury an internal scandal before the board saw the full audit trail."),
            IncidentDate = NormalizeIncidentDate(generated.IncidentDate),
            IncidentTime = NormalizeIncidentTime(generated.IncidentTime),
            BadgeDiscrepancy = ValueOrFallback(generated.BadgeDiscrepancy, "The physical badge log shows a different route than the digital access system recorded during the breach window."),
            VaultWord1 = vaultWords[0],
            VaultWord2 = vaultWords[1],
            VaultWord3 = vaultWords[2],
            VaultWord4 = vaultWords[3],
        };
    }

    private static List<Employee> NormalizeEmployees(List<Employee>? source)
    {
        if (source is null || source.Count == 0)
        {
            return FallbackEmployees.Select(item => new Employee
            {
                Id = item.Id,
                Name = item.Name,
                Department = item.Department,
                Role = item.Role,
                AccessLevel = item.AccessLevel,
            }).ToList();
        }

        var employees = new List<Employee>();
        var seenIds = new HashSet<int>();
        var nextFallbackIndex = 0;

        foreach (var sourceEmployee in source)
        {
            if (sourceEmployee is null) continue;

            var id = sourceEmployee.Id;
            if (id <= 0 || seenIds.Contains(id))
            {
                while (nextFallbackIndex < FallbackEmployees.Length && seenIds.Contains(FallbackEmployees[nextFallbackIndex].Id))
                {
                    nextFallbackIndex++;
                }

                id = nextFallbackIndex < FallbackEmployees.Length
                    ? FallbackEmployees[nextFallbackIndex].Id
                    : 2000 + employees.Count;
            }

            seenIds.Add(id);
            var fallback = FallbackEmployees.FirstOrDefault(item => item.Id == id);

            employees.Add(new Employee
            {
                Id = id,
                Name = ValueOrFallback(sourceEmployee.Name, fallback.Name == null ? $"Employee {id}" : fallback.Name),
                Department = ValueOrFallback(sourceEmployee.Department, fallback.Department == null ? "Security" : fallback.Department),
                Role = ValueOrFallback(sourceEmployee.Role, fallback.Role == null ? "Staff" : fallback.Role),
                AccessLevel = sourceEmployee.AccessLevel is >= 1 and <= 5 ? sourceEmployee.AccessLevel : (fallback.AccessLevel <= 0 ? 3 : fallback.AccessLevel),
            });
        }

        return employees.Count == 0 ? NormalizeEmployees(null) : employees;
    }

    private static int ResolveCulpritId(ClaudeGeneratedSession generated, List<Employee> employees)
    {
        if (generated.CulpritId > 0 && employees.Any(employee => employee.Id == generated.CulpritId))
        {
            return generated.CulpritId;
        }

        if (!string.IsNullOrWhiteSpace(generated.CulpritName))
        {
            var byName = employees.FirstOrDefault(employee => employee.Name.Equals(generated.CulpritName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byName is not null) return byName.Id;
        }

        return employees.First().Id;
    }

    private static Employee BuildFallbackEmployee(int culpritId)
    {
        var fallback = FallbackEmployees.FirstOrDefault(item => item.Id == culpritId);
        if (fallback.Id != 0)
        {
            return new Employee
            {
                Id = fallback.Id,
                Name = fallback.Name,
                Department = fallback.Department,
                Role = fallback.Role,
                AccessLevel = fallback.AccessLevel,
            };
        }

        return new Employee
        {
            Id = culpritId <= 0 ? 1001 : culpritId,
            Name = "Morgan Hale",
            Department = "Security",
            Role = "Security Manager",
            AccessLevel = 4,
        };
    }

    private static void EnsureAtLeastOneWitness(List<Employee> employees, int culpritId)
    {
        if (employees.Any(employee => employee.Id != culpritId)) return;

        var witness = FallbackEmployees.First(item => item.Id != culpritId);
        employees.Add(new Employee
        {
            Id = witness.Id,
            Name = witness.Name,
            Department = witness.Department,
            Role = witness.Role,
            AccessLevel = witness.AccessLevel,
        });
    }

    private static string[] NormalizeVaultWords(ClaudeGeneratedSession generated)
    {
        var rawWords = new[]
        {
            generated.VaultWord1,
            generated.VaultWord2,
            generated.VaultWord3,
            generated.VaultWord4,
        };

        var sanitized = new string[4];
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rawWords.Length; index++)
        {
            var candidate = NormalizeSingleWord(rawWords[index]);
            if (string.IsNullOrWhiteSpace(candidate) || used.Contains(candidate))
            {
                candidate = FallbackVaultWords.First(word => !used.Contains(word));
            }

            sanitized[index] = candidate;
            used.Add(candidate);
        }

        return sanitized;
    }

    private static string NormalizeSingleWord(string? value)
    {
        var cleaned = Regex.Replace(value ?? string.Empty, "[^a-zA-Z]", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .ToLowerInvariant();

        return cleaned ?? string.Empty;
    }

    private static string NormalizeIncidentDate(string? value)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return "2025-03-03";
    }

    private static string NormalizeIncidentTime(string? value)
    {
        if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        return "01:17";
    }

    private static string ValueOrFallback(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}