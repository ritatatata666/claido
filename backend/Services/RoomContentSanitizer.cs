using System.Text;
using System.Text.Json;
using Claido.Models;

namespace Claido.Services;

public static class RoomContentSanitizer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Normalize(string roomName, string rawJson, SessionState session)
    {
        return roomName switch
        {
            "shell" => JsonSerializer.Serialize(NormalizeShell(TryDeserialize<ShellRoomContent>(rawJson), session), JsonOptions),
            "mail" => JsonSerializer.Serialize(NormalizeMail(TryDeserialize<List<MailItem>>(rawJson), session), JsonOptions),
            "wiki" => JsonSerializer.Serialize(NormalizeWiki(TryDeserialize<List<WikiPage>>(rawJson), session), JsonOptions),
            "search" => JsonSerializer.Serialize(NormalizeSearch(TryDeserialize<List<SearchLog>>(rawJson), session), JsonOptions),
            "onion" => JsonSerializer.Serialize(NormalizeOnion(TryDeserialize<OnionRoomContent>(rawJson), session), JsonOptions),
            _ => rawJson,
        };
    }

    private static T? TryDeserialize<T>(string rawJson)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return default;
        }
    }

    private static ShellRoomContent NormalizeShell(ShellRoomContent? source, SessionState session)
    {
        var username = string.IsNullOrWhiteSpace(source?.Username) ? "analyst" : source!.Username.Trim();
        var home = $"/home/{username}";
        var files = source?.Files is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(source.Files, StringComparer.OrdinalIgnoreCase);

        var envPath = $"{home}/.env";
        var encodedWord = Convert.ToBase64String(Encoding.UTF8.GetBytes(session.VaultWord1));
        files[envPath] = UpsertLine(files.GetValueOrDefault(envPath), "VAULT_WORD", encodedWord,
            $"VAULT_WORD={encodedWord}\nDB_PASS=hunter2\nAPI_KEY=sk-fake-abc123");

        var accessLogPath = $"{home}/logs/access.log";
        var suspiciousEntry = $"[{ExtractTime(session.IncidentTimestamp)}] WARN  Employee {session.Culprit.Id} accessed Server Room B (AFTER_HOURS)";
        var accessLog = files.GetValueOrDefault(accessLogPath) ?? string.Empty;
        if (!accessLog.Contains($"Employee {session.Culprit.Id}", StringComparison.OrdinalIgnoreCase))
        {
            accessLog = string.IsNullOrWhiteSpace(accessLog)
                ? suspiciousEntry
                : $"{suspiciousEntry}\n{accessLog.Trim()}";
        }
        files[accessLogPath] = accessLog;

        var dirs = new HashSet<string>(source?.Dirs ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
        {
            home,
            $"{home}/logs",
            "/etc",
            "/var/log",
            "/tmp",
        };

        return new ShellRoomContent
        {
            Hostname = string.IsNullOrWhiteSpace(source?.Hostname) ? "novacorp-srv-01" : source!.Hostname.Trim(),
            Username = username,
            Files = files,
            Dirs = dirs.OrderBy(path => path).ToList(),
        };
    }

    private static List<MailItem> NormalizeMail(List<MailItem>? source, SessionState session)
    {
        var items = source?.Where(item => item is not null).Select((item, index) => NormalizeMailItem(item!, index)).ToList()
            ?? new List<MailItem>();

        if (items.Count == 0)
        {
            return GetDefaultMail(session);
        }

        if (!items.Any(item => (item.Body ?? string.Empty).Contains(session.VaultWord2, StringComparison.OrdinalIgnoreCase)))
        {
            var target = items.FindIndex(item => (item.From ?? string.Empty).Contains("unknown", StringComparison.OrdinalIgnoreCase));
            if (target < 0) target = 0;
            items[target].Body = $"{items[target].Body?.Trim()} The word you need is \"{session.VaultWord2}\".".Trim();
        }

        return items;
    }

    private static MailItem NormalizeMailItem(MailItem item, int index)
    {
        return new MailItem
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? $"msg-{index + 1:D3}" : item.Id,
            From = string.IsNullOrWhiteSpace(item.From) ? "alerts@novacorp.com" : item.From,
            To = string.IsNullOrWhiteSpace(item.To) ? "analyst@novacorp.com" : item.To,
            Subject = string.IsNullOrWhiteSpace(item.Subject) ? "Internal Update" : item.Subject,
            Date = string.IsNullOrWhiteSpace(item.Date) ? "2025-03-02T09:00:00" : item.Date,
            Body = item.Body ?? string.Empty,
            IsRead = item.IsRead,
            Folder = string.IsNullOrWhiteSpace(item.Folder) ? "inbox" : item.Folder.ToLowerInvariant(),
        };
    }

    private static List<WikiPage> NormalizeWiki(List<WikiPage>? source, SessionState session)
    {
        var pages = source?.Where(page => page is not null).Select((page, index) => NormalizeWikiPage(page!, index)).ToList()
            ?? new List<WikiPage>();

        if (pages.Count == 0)
        {
            return GetDefaultWiki(session);
        }

        var redactedIndex = pages.FindIndex(page => page.HasRedacted);
        if (redactedIndex < 0)
        {
            redactedIndex = 0;
            pages[0].HasRedacted = true;
        }

        var redactedPage = pages[redactedIndex];
        var decoded = Rot13(redactedPage.RedactedSection ?? string.Empty);
        if (!decoded.Contains(session.VaultWord3, StringComparison.OrdinalIgnoreCase))
        {
            redactedPage.RedactedSection = Rot13(BuildWikiClueSentence(session.VaultWord3));
        }

        return pages;
    }

    private static WikiPage NormalizeWikiPage(WikiPage page, int index)
    {
        return new WikiPage
        {
            Id = string.IsNullOrWhiteSpace(page.Id) ? $"page-{index + 1:D3}" : page.Id,
            Title = string.IsNullOrWhiteSpace(page.Title) ? $"Page {index + 1}" : page.Title,
            Category = string.IsNullOrWhiteSpace(page.Category) ? "Security" : page.Category,
            LastModified = string.IsNullOrWhiteSpace(page.LastModified) ? "2025-03-01" : page.LastModified,
            Author = string.IsNullOrWhiteSpace(page.Author) ? "NovaWiki" : page.Author,
            Content = page.Content ?? string.Empty,
            HasRedacted = page.HasRedacted,
            RedactedSection = page.RedactedSection,
        };
    }

    private static List<SearchLog> NormalizeSearch(List<SearchLog>? source, SessionState session)
    {
        var whistleblowerUserId = ResolveWhistleblowerUserId(session);
        var logs = source?.Where(log => log is not null).Select((log, index) => NormalizeSearchLog(log!, index, whistleblowerUserId)).ToList()
            ?? new List<SearchLog>();

        if (logs.Count == 0)
        {
            return GetDefaultSearchLogs(session, whistleblowerUserId);
        }

        var whistleIndex = logs.FindIndex(log =>
            string.Equals(log.Level, "ERROR", StringComparison.OrdinalIgnoreCase)
            && string.Equals(log.User, whistleblowerUserId, StringComparison.OrdinalIgnoreCase)
            && (log.Message ?? string.Empty).Contains(session.VaultWord4, StringComparison.OrdinalIgnoreCase));

        if (whistleIndex >= 0)
        {
            logs[whistleIndex] = EnsureSearchClue(logs[whistleIndex], session, whistleblowerUserId);
            return logs;
        }

        var existingWhistleIndex = logs.FindIndex(log =>
            string.Equals(log.Level, "ERROR", StringComparison.OrdinalIgnoreCase)
            && (
                string.Equals(log.User, whistleblowerUserId, StringComparison.OrdinalIgnoreCase)
                || (log.Message ?? string.Empty).Contains("whistleblower", StringComparison.OrdinalIgnoreCase)));

        if (existingWhistleIndex >= 0)
        {
            logs[existingWhistleIndex] = EnsureSearchClue(logs[existingWhistleIndex], session, whistleblowerUserId);
            return logs;
        }

        var insertAt = Math.Min(35, logs.Count);
        logs.Insert(insertAt, BuildSearchClueLog(session, whistleblowerUserId, "log-whistle-001"));
        return logs;
    }

    private static SearchLog NormalizeSearchLog(SearchLog log, int index, string whistleblowerUserId)
    {
        var user = string.IsNullOrWhiteSpace(log.User) ? whistleblowerUserId : log.User.Trim();
        if (string.Equals(user, "whistleblower", StringComparison.OrdinalIgnoreCase))
        {
            user = whistleblowerUserId;
        }

        return new SearchLog
        {
            Id = string.IsNullOrWhiteSpace(log.Id) ? $"log-{index + 1:D3}" : log.Id,
            Timestamp = string.IsNullOrWhiteSpace(log.Timestamp) ? "2025-03-03T00:00:00" : log.Timestamp,
            Level = string.IsNullOrWhiteSpace(log.Level) ? "INFO" : log.Level.ToUpperInvariant(),
            Service = string.IsNullOrWhiteSpace(log.Service) ? "api" : log.Service.ToLowerInvariant(),
            User = user,
            Message = log.Message ?? string.Empty,
            Ip = string.IsNullOrWhiteSpace(log.Ip) ? "192.168.1.10" : log.Ip,
        };
    }

    private static SearchLog EnsureSearchClue(SearchLog log, SessionState session, string whistleblowerUserId)
    {
        var clueMessage = BuildSearchClueMessage(session);
        var existingMessage = log.Message ?? string.Empty;
        return new SearchLog
        {
            Id = string.IsNullOrWhiteSpace(log.Id) ? "log-whistle-001" : log.Id,
            Timestamp = string.IsNullOrWhiteSpace(log.Timestamp) ? "2025-03-03T01:33:00" : log.Timestamp,
            Level = "ERROR",
            Service = string.IsNullOrWhiteSpace(log.Service) ? "vault" : log.Service.ToLowerInvariant(),
            User = whistleblowerUserId,
            Message = existingMessage.Contains(session.VaultWord4, StringComparison.OrdinalIgnoreCase)
                && existingMessage.Contains(session.Culprit.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                ? existingMessage
                : clueMessage,
            Ip = string.IsNullOrWhiteSpace(log.Ip) ? "192.168.1.99" : log.Ip,
        };
    }

    private static OnionRoomContent NormalizeOnion(OnionRoomContent? source, SessionState session)
    {
        var forumPosts = source?.ForumPosts?.Where(post => post is not null).Select((post, index) => NormalizeForumPost(post!, index)).ToList()
            ?? new List<ForumPost>();
        var marketListings = source?.MarketListings?.Where(listing => listing is not null).Select((listing, index) => NormalizeMarketListing(listing!, index)).ToList()
            ?? new List<MarketListing>();

        if (forumPosts.Count == 0 && marketListings.Count == 0)
        {
            return GetDefaultOnion(session);
        }

        if (!marketListings.Any(listing =>
            $"{listing.Title} {listing.Description}".Contains(session.Culprit.Department, StringComparison.OrdinalIgnoreCase)))
        {
            if (marketListings.Count == 0)
            {
                marketListings.Add(BuildDepartmentListing(session, 1));
            }
            else
            {
                marketListings[0].Title = $"{marketListings[0].Title} - {session.Culprit.Department} access";
                marketListings[0].Description = $"{marketListings[0].Description} Linked to NovaCorp {session.Culprit.Department} department credentials.".Trim();
            }
        }

        return new OnionRoomContent
        {
            ForumPosts = forumPosts,
            MarketListings = marketListings,
        };
    }

    private static ForumPost NormalizeForumPost(ForumPost post, int index)
    {
        return new ForumPost
        {
            Id = string.IsNullOrWhiteSpace(post.Id) ? $"post-{index + 1:D3}" : post.Id,
            Handle = string.IsNullOrWhiteSpace(post.Handle) ? "anon_handle" : post.Handle,
            Timestamp = string.IsNullOrWhiteSpace(post.Timestamp) ? "2025-03-03T12:00:00" : post.Timestamp,
            Category = string.IsNullOrWhiteSpace(post.Category) ? "leaks" : post.Category,
            Title = string.IsNullOrWhiteSpace(post.Title) ? "Untitled thread" : post.Title,
            Body = post.Body ?? string.Empty,
            Replies = post.Replies,
        };
    }

    private static MarketListing NormalizeMarketListing(MarketListing listing, int index)
    {
        return new MarketListing
        {
            Id = string.IsNullOrWhiteSpace(listing.Id) ? $"listing-{index + 1:D3}" : listing.Id,
            Seller = string.IsNullOrWhiteSpace(listing.Seller) ? "unknown_vendor" : listing.Seller,
            Title = string.IsNullOrWhiteSpace(listing.Title) ? "Stolen credentials" : listing.Title,
            Price = string.IsNullOrWhiteSpace(listing.Price) ? "0.010 BTC" : listing.Price,
            Description = listing.Description ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(listing.Category) ? "credentials" : listing.Category,
        };
    }

    private static List<MailItem> GetDefaultMail(SessionState session)
    {
        return
        [
            new MailItem
            {
                Id = "msg-001",
                From = "unknown@protonmail.com",
                To = "analyst@novacorp.com",
                Subject = "Re: Tonight",
                Date = "2025-03-02T20:11:00",
                Body = $"Everything is ready for the handover. The word you need is \"{session.VaultWord2}\". Delete this.",
                Folder = "inbox",
            },
            new MailItem
            {
                Id = "msg-002",
                From = "hr@novacorp.com",
                To = "all@novacorp.com",
                Subject = "Reminder: Security audit",
                Date = "2025-03-01T09:30:00",
                Body = "All employees must complete access-control training by Friday.",
                Folder = "inbox",
            },
        ];
    }

    private static List<WikiPage> GetDefaultWiki(SessionState session)
    {
        return
        [
            new WikiPage
            {
                Id = "page-001",
                Title = "Server Room Access Protocol",
                Category = "Security",
                LastModified = "2025-03-01",
                Author = "Alex Torres",
                Content = "## Access Protocol\n\nAfter-hours access requires explicit approval.",
                HasRedacted = true,
                RedactedSection = Rot13(BuildWikiClueSentence(session.VaultWord3)),
            },
            new WikiPage
            {
                Id = "page-002",
                Title = "Employee Handbook",
                Category = "HR",
                LastModified = "2025-02-20",
                Author = "HR Team",
                Content = "## Welcome\n\nAll employees must badge in and out.",
            },
        ];
    }

    private static List<SearchLog> GetDefaultSearchLogs(SessionState session, string whistleblowerUserId)
    {
        var logs = new List<SearchLog>();
        for (var index = 1; index <= 49; index++)
        {
            logs.Add(new SearchLog
            {
                Id = $"log-{index:D3}",
                Timestamp = $"2025-03-03T{(index % 24):D2}:{(index * 7 % 60):D2}:00",
                Level = index % 5 == 0 ? "WARN" : "INFO",
                Service = index % 3 == 0 ? "auth" : "api",
                User = session.Employees.FirstOrDefault(employee => employee.Id != session.Culprit.Id)?.Id.ToString() ?? whistleblowerUserId,
                Message = "Normal system operation",
                Ip = $"192.168.1.{10 + index}",
            });
        }

        logs.Insert(Math.Min(35, logs.Count), BuildSearchClueLog(session, whistleblowerUserId, "log-050"));
        return logs;
    }

    private static SearchLog BuildSearchClueLog(SessionState session, string whistleblowerUserId, string id)
    {
        return new SearchLog
        {
            Id = id,
            Timestamp = "2025-03-03T01:33:00",
            Level = "ERROR",
            Service = "vault",
            User = whistleblowerUserId,
            Message = BuildSearchClueMessage(session),
            Ip = "192.168.1.99",
        };
    }

    private static string BuildSearchClueMessage(SessionState session)
    {
        return $"ALERT: confidential witness report linked employee {session.Culprit.Id} to unauthorized vault activity. Motive keyword: {session.VaultWord4}.";
    }

    private static OnionRoomContent GetDefaultOnion(SessionState session)
    {
        return new OnionRoomContent
        {
            ForumPosts =
            [
                new ForumPost
                {
                    Id = "post-001",
                    Handle = "g0stwr1ter",
                    Timestamp = "2025-03-03T14:05:00",
                    Category = "leaks",
                    Title = "Did something happen at NovaCorp last night?",
                    Body = "Multiple encrypted comms were observed between 01:00 and 03:00. Someone inside NovaCorp is selling access.",
                    Replies = 7,
                },
            ],
            MarketListings =
            [
                BuildDepartmentListing(session, 1),
            ],
        };
    }

    private static MarketListing BuildDepartmentListing(SessionState session, int index)
    {
        return new MarketListing
        {
            Id = $"listing-{index:D3}",
            Seller = "xd4rk_n3t",
            Title = $"NovaCorp {session.Culprit.Department} Dept - Level 5 Access",
            Price = "0.045 BTC",
            Description = $"Fresh credentials for a NovaCorp {session.Culprit.Department.ToLowerInvariant()} department account. High clearance. Vault access included.",
            Category = "credentials",
        };
    }

    private static string ResolveWhistleblowerUserId(SessionState session)
    {
        return session.Employees.FirstOrDefault(employee => employee.Id != session.Culprit.Id)?.Id.ToString()
            ?? session.Employees.First().Id.ToString();
    }

    private static string BuildWikiClueSentence(string vaultWord)
    {
        return $"Security appendix: operators replaced employee names with token {vaultWord} in the restricted transcript.";
    }

    private static string Rot13(string value)
    {
        return new string(value.Select(character => character switch
        {
            >= 'a' and <= 'z' => (char)('a' + (character - 'a' + 13) % 26),
            >= 'A' and <= 'Z' => (char)('A' + (character - 'A' + 13) % 26),
            _ => character,
        }).ToArray());
    }

    private static string ExtractTime(string incidentTimestamp)
    {
        if (DateTime.TryParse(incidentTimestamp, out var parsed))
        {
            return parsed.ToString("HH:mm:ss");
        }

        return "01:17:00";
    }

    private static string UpsertLine(string? content, string key, string value, string fallbackContent)
    {
        if (string.IsNullOrWhiteSpace(content)) return fallbackContent;

        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        var index = lines.FindIndex(line => line.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            lines[index] = $"{key}={value}";
        }
        else
        {
            lines.Insert(0, $"{key}={value}");
        }

        return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private sealed class ShellRoomContent
    {
        public string Hostname { get; set; } = "";
        public string Username { get; set; } = "";
        public Dictionary<string, string> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Dirs { get; set; } = new();
    }

    private sealed class MailItem
    {
        public string Id { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Date { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsRead { get; set; }
        public string Folder { get; set; } = "";
    }

    private sealed class WikiPage
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string LastModified { get; set; } = "";
        public string Author { get; set; } = "";
        public string Content { get; set; } = "";
        public bool HasRedacted { get; set; }
        public string? RedactedSection { get; set; }
    }

    private sealed class SearchLog
    {
        public string Id { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string Level { get; set; } = "";
        public string Service { get; set; } = "";
        public string User { get; set; } = "";
        public string Message { get; set; } = "";
        public string Ip { get; set; } = "";
    }

    private sealed class OnionRoomContent
    {
        public List<ForumPost> ForumPosts { get; set; } = new();
        public List<MarketListing> MarketListings { get; set; } = new();
    }

    private sealed class ForumPost
    {
        public string Id { get; set; } = "";
        public string Handle { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public int Replies { get; set; }
    }

    private sealed class MarketListing
    {
        public string Id { get; set; } = "";
        public string Seller { get; set; } = "";
        public string Title { get; set; } = "";
        public string Price { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
    }
}