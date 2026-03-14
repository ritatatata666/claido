using Claido.Models;
using Microsoft.Data.Sqlite;

namespace Claido.Services;

public static class DatabaseService
{
    public static string GenerateSqliteBase64(SessionState session)
    {
        var dbPath = Path.GetTempFileName();
        try
        {
            File.Delete(dbPath);
            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString();
            using (var conn = new SqliteConnection(connStr))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();

                // Create tables
                cmd.CommandText = """
                    CREATE TABLE employees (
                        id INTEGER PRIMARY KEY,
                        name TEXT NOT NULL,
                        department TEXT NOT NULL,
                        role TEXT NOT NULL,
                        access_level INTEGER NOT NULL,
                        email TEXT NOT NULL,
                        hire_date TEXT NOT NULL,
                        status TEXT NOT NULL DEFAULT 'active'
                    );

                    CREATE TABLE access_logs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        employee_id INTEGER NOT NULL,
                        location TEXT NOT NULL,
                        action TEXT NOT NULL,
                        timestamp TEXT NOT NULL,
                        badge_id TEXT NOT NULL,
                        success INTEGER NOT NULL DEFAULT 1,
                        notes TEXT
                    );

                    CREATE TABLE incidents (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT NOT NULL,
                        severity TEXT NOT NULL,
                        reported_by TEXT NOT NULL,
                        description TEXT NOT NULL,
                        timestamp TEXT NOT NULL,
                        status TEXT NOT NULL,
                        related_employee_id INTEGER
                    );

                    CREATE TABLE messages (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        sender_id INTEGER NOT NULL,
                        recipient_id INTEGER NOT NULL,
                        subject TEXT NOT NULL,
                        body TEXT NOT NULL,
                        sent_at TEXT NOT NULL,
                        deleted INTEGER NOT NULL DEFAULT 0
                    );
                    """;
                cmd.ExecuteNonQuery();

                // Insert employees
                foreach (var emp in session.Employees)
                {
                    var dept = emp.Department.ToLower().Replace(" ", "");
                    cmd.CommandText = """
                        INSERT INTO employees (id, name, department, role, access_level, email, hire_date, status)
                        VALUES (@id, @name, @dept, @role, @al, @email, @hire, @status)
                        """;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", emp.Id);
                    cmd.Parameters.AddWithValue("@name", emp.Name);
                    cmd.Parameters.AddWithValue("@dept", emp.Department);
                    cmd.Parameters.AddWithValue("@role", emp.Role);
                    cmd.Parameters.AddWithValue("@al", emp.AccessLevel);
                    cmd.Parameters.AddWithValue("@email", $"{emp.Name.Split(' ')[0].ToLower()}@novacorp.com");
                    cmd.Parameters.AddWithValue("@hire", $"202{Random.Shared.Next(0, 5)}-0{Random.Shared.Next(1, 9)}-{Random.Shared.Next(10, 28):D2}");
                    cmd.Parameters.AddWithValue("@status", emp.Id == session.Culprit.Id ? "active" : "active");
                    cmd.ExecuteNonQuery();
                }

                // Insert access logs — culprit has suspicious entry
                var suspiciousTime = session.IncidentTimestamp;
                cmd.Parameters.Clear();
                var logEntries = new List<(int empId, string location, string action, string ts, string badge, int success, string notes)>
                {
                    (session.Culprit.Id, "Server Room B", "DOOR_UNLOCK", suspiciousTime, $"BADGE-{session.Culprit.Id}", 1, "After-hours access"),
                    (session.Culprit.Id, "Server Room B", "TERMINAL_LOGIN", suspiciousTime, $"BADGE-{session.Culprit.Id}", 1, null!),
                    (session.Culprit.Id, "Executive Floor", "DOOR_UNLOCK", "2025-03-02T22:15:00", $"BADGE-{session.Culprit.Id}", 1, null!),
                    (session.Culprit.Id, "Main Entrance", "BADGE_SCAN", "2025-03-03T03:47:00", $"BADGE-{session.Culprit.Id}", 1, "Exit after incident"),
                };

                // Add normal entries for other employees
                var locations = new[] { "Main Entrance", "Cafeteria", "Engineering Floor", "HR Office", "Finance Wing" };
                var actions = new[] { "DOOR_UNLOCK", "BADGE_SCAN", "TERMINAL_LOGIN", "PRINTER_USE" };
                var rng = Random.Shared;
                foreach (var emp in session.Employees.Where(e => e.Id != session.Culprit.Id).Take(5))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        logEntries.Add((emp.Id,
                            locations[rng.Next(locations.Length)],
                            actions[rng.Next(actions.Length)],
                            $"2025-03-03T{rng.Next(8, 18):D2}:{rng.Next(0, 59):D2}:00",
                            $"BADGE-{emp.Id}",
                            1,
                            null!));
                    }
                }

                foreach (var (empId, loc, action, ts, badge, success, notes) in logEntries)
                {
                    cmd.CommandText = """
                        INSERT INTO access_logs (employee_id, location, action, timestamp, badge_id, success, notes)
                        VALUES (@eid, @loc, @action, @ts, @badge, @success, @notes)
                        """;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@eid", empId);
                    cmd.Parameters.AddWithValue("@loc", loc);
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@ts", ts);
                    cmd.Parameters.AddWithValue("@badge", badge);
                    cmd.Parameters.AddWithValue("@success", success);
                    cmd.Parameters.AddWithValue("@notes", notes ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Insert incidents
                var incidentRows = new[]
                {
                    ("Unauthorized Server Room Access", "CRITICAL", "Security System", $"Employee ID {session.Culprit.Id} accessed Server Room B outside authorized hours.", suspiciousTime, "OPEN", session.Culprit.Id),
                    ("Badge System Offline", "HIGH", "IT Ops", "Badge verification system was offline 01:00-03:00 on 2025-03-03.", "2025-03-03T01:00:00", "RESOLVED", (int?)null),
                    ("Failed Login Attempts", "MEDIUM", "SIEM", "Multiple failed login attempts on vault terminal.", "2025-03-02T23:45:00", "INVESTIGATING", (int?)null),
                };

                foreach (var (title, sev, reporter, desc, ts, status, relEmp) in incidentRows)
                {
                    cmd.CommandText = """
                        INSERT INTO incidents (title, severity, reported_by, description, timestamp, status, related_employee_id)
                        VALUES (@title, @sev, @reporter, @desc, @ts, @status, @rel)
                        """;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@sev", sev);
                    cmd.Parameters.AddWithValue("@reporter", reporter);
                    cmd.Parameters.AddWithValue("@desc", desc);
                    cmd.Parameters.AddWithValue("@ts", ts);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@rel", relEmp.HasValue ? (object)relEmp.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Insert messages
                var msgRows = new[]
                {
                    (session.Culprit.Id, 1002, "Re: Tonight", "Don't message me on this channel. I'll handle it. Delete this.", "2025-03-02T20:11:00"),
                    (1002, session.Culprit.Id, "Re: Tonight", "Are you sure? The risk is too high.", "2025-03-02T20:05:00"),
                    (session.Culprit.Id, 1003, "Project Update", "Everything is on schedule. No concerns.", "2025-03-02T15:30:00"),
                };

                foreach (var (sender, recip, subj, body, ts) in msgRows)
                {
                    cmd.CommandText = """
                        INSERT INTO messages (sender_id, recipient_id, subject, body, sent_at)
                        VALUES (@s, @r, @subj, @body, @ts)
                        """;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@s", sender);
                    cmd.Parameters.AddWithValue("@r", recip);
                    cmd.Parameters.AddWithValue("@subj", subj);
                    cmd.Parameters.AddWithValue("@body", body);
                    cmd.Parameters.AddWithValue("@ts", ts);
                    cmd.ExecuteNonQuery();
                }
            }

            var bytes = File.ReadAllBytes(dbPath);
            return Convert.ToBase64String(bytes);
        }
        finally
        {
            // Ensure no pooled SQLite handle keeps temp files locked on Windows.
            SqliteConnection.ClearAllPools();
            TryDeleteSqliteFiles(dbPath);
        }
    }

    private static void TryDeleteSqliteFiles(string dbPath)
    {
        var paths = new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" };

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    File.Delete(path);
                    break;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(25);
                }
                catch (UnauthorizedAccessException) when (attempt < 2)
                {
                    Thread.Sleep(25);
                }
            }
        }
    }
}
