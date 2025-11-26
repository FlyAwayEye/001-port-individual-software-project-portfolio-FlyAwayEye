using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

Console.Title = "Main Menu";

var db = new Database();
db.Initialize();

var currentUser = LoginFlow(db);
if (currentUser is null)
{
    // LoginFlow will exit on Escape; null is unexpected but handled defensively.
    Console.WriteLine("No user logged in. Exiting...");
    return;
}

while (true)
{
    Console.Clear();
    Console.WriteLine($"Logged in as: {currentUser.FirstName} {currentUser.LastName} - {currentUser.Type}");
    Console.WriteLine();
    Console.WriteLine("Please choose an option by pressing the corresponding number (Escape to exit):");
    Console.WriteLine();

    // Only show "Make a report" to Students (hide for PersonalSupervisor and SeniorTutor)
    if (currentUser.Type == UserType.Student)
    {
        Console.WriteLine("1) Make a report");
    }

    Console.WriteLine("2) Book a meeting");
    Console.WriteLine("3) Database");
    // Show "Tutor Activity Tracker" on main menu only for Senior Tutors
    if (currentUser.Type == UserType.SeniorTutor)
    {
        Console.WriteLine("4) Tutor Activity Tracker");
    }
    Console.WriteLine();
    Console.Write("Select an option: ");

    var keyInfo = ReadKeyAllowEscape(intercept: true);
    Console.WriteLine(); // move to next line after key press

    switch (keyInfo.KeyChar)
    {
        case '1':
            // Defensive guard - only students are allowed
            if (currentUser.Type == UserType.Student)
                MakeReportFlow(db, currentUser);
            else
            {
                Console.WriteLine("Invalid selection. Press any key to return to the menu.");
                ReadKeyAllowEscape(intercept: true);
            }
            break;
        case '2':
            BookMeetingFlow(db, currentUser);
            break;
        case '3':
            ShowDatabaseMenu(db, currentUser);
            break;
        case '4':
            // Only Senior Tutors should be able to see and use this option; guard again defensively.
            if (currentUser.Type == UserType.SeniorTutor)
                ShowTutorActivityTracker(db, currentUser);
            else
            {
                Console.WriteLine("Invalid selection. Press any key to return to the menu.");
                ReadKeyAllowEscape(intercept: true);
            }
            break;
        default:
            Console.WriteLine("Invalid selection. Press any key to return to the menu.");
            ReadKeyAllowEscape(intercept: true);
            break;
    }
}
// Method for exiting program at anytime
static void ExitNow()
{
    Console.WriteLine();
    Console.WriteLine("Exiting...");
    Environment.Exit(0);
}

static ConsoleKeyInfo ReadKeyAllowEscape(bool intercept = false)
{
    var ki = Console.ReadKey(intercept);
    if (ki.Key == ConsoleKey.Escape)
        ExitNow();
    return ki;
}

/// <summary>
/// Reads a single-line input from the console while allowing Escape to exit.
/// If <paramref name="initial"/> is provided the input buffer is pre-filled and displayed so the user can edit it.
/// Basic backspace handling is implemented.
/// </summary>
static string? ReadLineAllowEscape(string? initial = null)
{
    var sb = new StringBuilder();
    if (!string.IsNullOrEmpty(initial))
    {
        sb.Append(initial);
        Console.Write(initial);
    }

    while (true)
    {
        var ki = Console.ReadKey(intercept: true);

        if (ki.Key == ConsoleKey.Escape)
            ExitNow();

        if (ki.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return sb.ToString();
        }

        if (ki.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                // remove last char from buffer and from console
                sb.Length--;
                Console.Write("\b \b");
            }
            continue;
        }

        // ignore non-printable keys
        if (char.IsControl(ki.KeyChar))
            continue;

        sb.Append(ki.KeyChar);
        Console.Write(ki.KeyChar);
    }
}

static User? LoginFlow(Database db)
{
    Console.Clear();
    Console.WriteLine("Please log in with your 4-digit user ID.");
    Console.WriteLine("Press Escape anytime to quit.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("User ID: ");
        var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Empty input. Try again.");
            continue;
        }

        if (!IsValidFourDigitCode(input))
        {
            Console.WriteLine("Invalid format. The login code must be exactly 4 digits (0-9). Try again.");
            continue;
        }

        var users = db.GetAllUsers();
        var match = users.FirstOrDefault(u => string.Equals(u.LoginCode, input, StringComparison.Ordinal));
        if (match is not null)
        {
            Console.WriteLine();
            Console.WriteLine($"Welcome, {match.FirstName} {match.LastName} ({match.Type}). Press any key to continue.");
            ReadKeyAllowEscape(intercept: true);
            return match;
        }

        Console.WriteLine("No user found with that ID. Try again.");
    }
}

static void MakeReportFlow(Database db, User currentUser)
{
    Console.Clear();
    Console.WriteLine("Make a Report (Escape to exit)");
    Console.WriteLine();

    if (currentUser.Type != UserType.Student)
    {
        Console.WriteLine("Only users with type 'Student' can create reports.");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to the main menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Ask for the 4-digit student ID and verify it maps to a student record.
    Console.Write("Enter your 4-digit student ID to confirm: ");
    var idInput = ReadLineAllowEscape()?.Trim() ?? string.Empty;
    if (!IsValidFourDigitCode(idInput))
    {
        Console.WriteLine("Invalid ID format. The login code must be exactly 4 digits. Returning to menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    var users = db.GetAllUsers();
    var student = users.FirstOrDefault(u => string.Equals(u.LoginCode, idInput, StringComparison.Ordinal));
    if (student is null || student.Type != UserType.Student)
    {
        Console.WriteLine("No student found with that ID. Returning to menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Ensure the entered ID matches the logged-in user (identity confirmation).
    if (!string.Equals(currentUser.LoginCode, idInput, StringComparison.Ordinal))
    {
        Console.WriteLine("Entered student ID does not match the logged-in student. Returning to menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Collect report text with edit loop. Pre-fill editor with existing text when editing.
    string reportText = string.Empty;
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Enter your report. Press Enter to submit:");
        Console.Write("> ");
        reportText = ReadLineAllowEscape(reportText) ?? string.Empty; // initial empty on first pass

        Console.WriteLine();
        Console.Write("Would you like to make changes to the report before submission? (y/n): ");
        var confirm = ReadKeyAllowEscape(intercept: true);
        Console.WriteLine();

        var keyChar = char.ToLowerInvariant(confirm.KeyChar);
        if (keyChar == 'y')
        {
            // Loop back and let them edit the current reportText (pre-filled).
            continue;
        }

        if (keyChar == 'n')
        {
            break;
        }

        // If something else was pressed, ask again
        Console.WriteLine("Invalid response. Please enter 'y' or 'n'.");
    }

    // Prepare report entry and append to file
    try
    {
        var path = GetReportsFilePath();
        var entry = BuildReportEntry(student, reportText);
        // Ensure file exists and append entry
        File.AppendAllText(path, entry);
        Console.WriteLine();
        Console.WriteLine($"Report saved to {Path.GetFileName(path)}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Failed to save report: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the main menu.");
    ReadKeyAllowEscape(intercept: true);
}

static void BookMeetingFlow(Database db, User currentUser)
{
    Console.Clear();
    Console.WriteLine("Book a Meeting (Escape to exit)");
    Console.WriteLine();

    // Only students and personal supervisors can book meetings
    if (currentUser.Type != UserType.Student && currentUser.Type != UserType.PersonalSupervisor)
    {
        Console.WriteLine("Only students and personal supervisors can book meetings.");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to the main menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Confirm identity with login ID
    Console.Write("Enter your 4-digit login ID to confirm: ");
    var idInput = ReadLineAllowEscape()?.Trim() ?? string.Empty;
    if (!IsValidFourDigitCode(idInput) || !string.Equals(idInput, currentUser.LoginCode, StringComparison.Ordinal))
    {
        Console.WriteLine("Invalid or non-matching login ID. Returning to menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Find potential targets (students <-> personal supervisors)
    var allUsers = db.GetAllUsers();
    IEnumerable<User> targets;
    if (currentUser.Type == UserType.Student)
    {
        targets = allUsers.Where(u => u.Type == UserType.PersonalSupervisor).ToList();
    }
    else // personal supervisor booking
    {
        targets = allUsers.Where(u => u.Type == UserType.Student).ToList();
    }

    var targetsList = targets.ToList();
    if (targetsList.Count == 0)
    {
        Console.WriteLine("No matching users available to book a meeting with.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    // Read existing meetings to show counts
    var meetings = ReadMeetings();

    Console.WriteLine();
    Console.WriteLine("Select a user to book a meeting with:");
    for (int i = 0; i < targetsList.Count; i++)
    {
        var t = targetsList[i];
        // count existing meetings between currentUser and this target
        var existingCount = meetings.Count(m => string.Equals(m.BookerId, currentUser.LoginCode, StringComparison.Ordinal)
                                               && string.Equals(m.TargetId, t.LoginCode, StringComparison.Ordinal));
        Console.WriteLine($"{i + 1}) {t.FirstName} {t.LastName} ({t.LoginCode}) - Existing meetings with them: {existingCount}");
    }

    Console.WriteLine();
    Console.Write("Select a number to choose a user: ");
    var selText = ReadLineAllowEscape()?.Trim() ?? string.Empty;
    if (!int.TryParse(selText, out var sel) || sel < 1 || sel > targetsList.Count)
    {
        Console.WriteLine("Invalid selection. Returning to menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    var chosen = targetsList[sel - 1];

    // Choose date
    DateTime date;
    while (true)
    {
        Console.Write("Enter meeting date (DD/MM/YY): ");
        var dateText = ReadLineAllowEscape()?.Trim() ?? string.Empty;
        if (DateTime.TryParseExact(dateText, new[] { "dd/MM/yy", "d/M/yy", "dd/MM/yyyy" }, null, System.Globalization.DateTimeStyles.None, out date))
        {
            // accept parsed date
            break;
        }

        Console.WriteLine("Invalid date format. Use DD/MM/YY. Try again.");
    }

    // Choose time (HH:mm) and validate range 07:30 - 18:00
    TimeSpan timeOfDay;
    var earliest = new TimeSpan(7, 30, 0);
    var latest = new TimeSpan(18, 0, 0);
    while (true)
    {
        Console.Write("Enter meeting time (HH:mm, 24-hour, e.g. 17:00): ");
        var timeText = ReadLineAllowEscape()?.Trim() ?? string.Empty;
        if (TimeSpan.TryParseExact(timeText, "hh\\:mm", null, out timeOfDay) ||
            TimeSpan.TryParseExact(timeText, "h\\:mm", null, out timeOfDay))
        {
            if (timeOfDay < earliest || timeOfDay > latest)
            {
                Console.WriteLine("Time must be between 07:30 and 18:00. Try again.");
                continue;
            }

            break;
        }

        Console.WriteLine("Invalid time format. Use HH:mm (24-hour). Try again.");
    }

    // Collect reason with edit loop (same UX as reports)
    string reason = string.Empty;
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Enter reason for meeting. Press Enter to submit:");
        Console.Write("> ");
        reason = ReadLineAllowEscape(reason) ?? string.Empty;

        Console.WriteLine();
        Console.Write("Would you like to make changes to the reason before submission? (y/n): ");
        var confirm = ReadKeyAllowEscape(intercept: true);
        Console.WriteLine();

        var keyChar = char.ToLowerInvariant(confirm.KeyChar);
        if (keyChar == 'y')
        {
            // loop back with reason pre-filled
            continue;
        }

        if (keyChar == 'n')
        {
            break;
        }

        Console.WriteLine("Invalid response. Please enter 'y' or 'n'.");
    }

    // Create meeting entry and assign per-pair index (how many meetings between these two already)
    var pairCount = meetings.Count(m => string.Equals(m.BookerId, currentUser.LoginCode, StringComparison.Ordinal)
                                       && string.Equals(m.TargetId, chosen.LoginCode, StringComparison.Ordinal));
    var meeting = new MeetingEntry
    {
        BookerId = currentUser.LoginCode,
        BookerName = $"{currentUser.FirstName} {currentUser.LastName}",
        TargetId = chosen.LoginCode,
        TargetName = $"{chosen.FirstName} {chosen.LastName}",
        MeetingDate = date.Date + timeOfDay,
        PerPairIndex = pairCount + 1,
        CreatedAt = DateTime.UtcNow,
        Reason = reason
    };

    try
    {
        var path = GetMeetingsFilePath();
        var entryText = BuildMeetingEntry(meeting);
        File.AppendAllText(path, entryText);
        Console.WriteLine();
        Console.WriteLine("Meeting saved to Meetings.txt.");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Failed to save meeting: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the main menu.");
    ReadKeyAllowEscape(intercept: true);
}

static string GetMeetingsFilePath() =>
    Path.Combine(AppContext.BaseDirectory ?? ".", "Meetings.txt");

static string BuildMeetingEntry(MeetingEntry m)
{
    var sb = new StringBuilder();
    sb.AppendLine("----- MEETING START -----");
    sb.AppendLine($"CreatedAt: {m.CreatedAt:O}");
    sb.AppendLine($"Booker: {m.BookerName}");
    sb.AppendLine($"Booker ID: {m.BookerId}");
    sb.AppendLine($"Target: {m.TargetName}");
    sb.AppendLine($"Target ID: {m.TargetId}");
    sb.AppendLine($"Date: {m.MeetingDate:dd/MM/yy}");
    sb.AppendLine($"Time: {m.MeetingDate:HH:mm}");
    sb.AppendLine($"Reason: {m.Reason}");
    sb.AppendLine($"PairIndex: {m.PerPairIndex}");
    sb.AppendLine("----- MEETING END -----");
    sb.AppendLine();
    return sb.ToString();
}

static List<MeetingEntry> ReadMeetings()
{
    var path = GetMeetingsFilePath();
    var list = new List<MeetingEntry>();
    if (!File.Exists(path))
        return list;

    var text = File.ReadAllText(path);
    var parts = text.Split(new[] { "----- MEETING START -----" }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
        var trimmed = part.Trim();
        if (string.IsNullOrEmpty(trimmed)) continue;

        string GetValue(string prefix)
        {
            var idx = trimmed.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;
            var start = idx + prefix.Length;
            var end = trimmed.IndexOf('\n', start);
            if (end < 0) end = trimmed.Length;
            return trimmed[start..end].Trim();
        }

        var createdAtText = GetValue("CreatedAt: ");
        var booker = GetValue("Booker: ");
        var bookerId = GetValue("Booker ID: ");
        var target = GetValue("Target: ");
        var targetId = GetValue("Target ID: ");
        var dateText = GetValue("Date: ");
        var timeText = GetValue("Time: ");
        var reasonText = GetValue("Reason: ");
        var pairIndexText = GetValue("PairIndex: ");

        DateTime meetingDate = DateTime.MinValue;
        if (!string.IsNullOrEmpty(dateText) && !string.IsNullOrEmpty(timeText))
        {
            if (!DateTime.TryParseExact($"{dateText} {timeText}",
                    new[] { "dd/MM/yy HH:mm", "d/M/yy HH:mm", "dd/MM/yyyy HH:mm" }, null,
                    System.Globalization.DateTimeStyles.None, out meetingDate))
            {
                meetingDate = DateTime.MinValue;
            }
        }

        DateTime createdAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(createdAtText))
        {
            DateTime.TryParse(createdAtText, null, System.Globalization.DateTimeStyles.RoundtripKind, out createdAt);
        }

        if (string.IsNullOrEmpty(bookerId) || string.IsNullOrEmpty(targetId))
            continue;

        if (!int.TryParse(pairIndexText, out var pairIndex))
            pairIndex = 0;

        list.Add(new MeetingEntry
        {
            BookerId = bookerId,
            BookerName = booker,
            TargetId = targetId,
            TargetName = target,
            MeetingDate = meetingDate,
            PerPairIndex = pairIndex,
            CreatedAt = createdAt,
            Reason = reasonText
        });
    }

    return list;
}

static string GetReportsFilePath() =>
    Path.Combine(AppContext.BaseDirectory ?? ".", "student_reports.txt");

static string BuildReportEntry(User student, string reportText)
{
    var sb = new StringBuilder();
    sb.AppendLine("----- REPORT START -----");
    sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
    sb.AppendLine($"Student: {student.FirstName} {student.LastName}");
    sb.AppendLine($"Student ID: {student.LoginCode}");
    sb.AppendLine("Report:");
    sb.AppendLine(reportText);
    sb.AppendLine("----- REPORT END -----");
    sb.AppendLine();
    return sb.ToString();
}

static void ShowDatabaseMenu(Database db, User currentUser)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("Database Menu (Escape to exit)");
        Console.WriteLine();

        // Build visible options dynamically based on currentUser.Type
        var options = new List<(string Label, Action Action)>();

        // Add "Add a new user" only for non-students
        if (currentUser.Type != UserType.Student)
            options.Add(("Add a new user", () => AddUserFlow(db)));

        options.Add(("List users", () => ListUsersFlow(db)));
        options.Add(("Reports", ShowReportsMenu));
        options.Add(("Meetings", ShowMeetingsMenu));

        if (currentUser.Type == UserType.PersonalSupervisor || currentUser.Type == UserType.SeniorTutor)
            options.Add(("Student activity", () => ShowStudentActivityMenu(db)));

        // Display options with runtime numbering
        for (int i = 0; i < options.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {options[i].Label}");
        }

        Console.WriteLine();
        Console.WriteLine("0) Back to main menu");
        Console.WriteLine();
        Console.Write("Select an option: ");

        var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;
        if (!int.TryParse(input, out var sel))
        {
            Console.WriteLine("Invalid selection. Press any key to continue (Escape to exit).");
            ReadKeyAllowEscape(intercept: true);
            continue;
        }

        if (sel == 0)
            return;

        if (sel < 1 || sel > options.Count)
        {
            Console.WriteLine("Selection out of range. Press any key to continue (Escape to exit).");
            ReadKeyAllowEscape(intercept: true);
            continue;
        }

        // Invoke selected action
        options[sel - 1].Action();
    }

    // Local function to show reports
    void ShowReportsMenu()
    {
        while (true)
        {
            Console.Clear();
            var reports = ReadReports();

            // If logged in user is a student, filter to only their reports
            if (currentUser.Type == UserType.Student)
                reports = reports.Where(r => string.Equals(r.StudentId, currentUser.LoginCode, StringComparison.Ordinal)).ToList();

            Console.WriteLine("Reports (Escape to exit)");
            if (currentUser.Type == UserType.Student)
                Console.WriteLine("(showing only your reports)");
            Console.WriteLine();

            if (reports.Count == 0)
            {
                Console.WriteLine("(no reports)");
                Console.WriteLine();
                Console.WriteLine("Press any key to return to Database menu.");
                ReadKeyAllowEscape(intercept: true);
                return;
            }

            for (int i = 0; i < reports.Count; i++)
            {
                var r = reports[i];
                Console.WriteLine($"{i + 1}) {r.StudentName} ({r.StudentId}) - Report {r.PerUserIndex} - {r.Timestamp:yyyy-MM-dd}");
            }

            Console.WriteLine();
            Console.Write("Select a report number to view or 0 to return: ");
            var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;
            if (!int.TryParse(input, out var selection))
            {
                Console.WriteLine("Invalid selection. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            if (selection == 0)
                return;

            if (selection < 1 || selection > reports.Count)
            {
                Console.WriteLine("Selection out of range. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            // Show the selected report
            var selected = reports[selection - 1];
            Console.Clear();
            Console.WriteLine("----- REPORT -----");
            Console.WriteLine($"Timestamp: {selected.Timestamp:O}");
            Console.WriteLine($"Student : {selected.StudentName}");
            Console.WriteLine($"Student ID: {selected.StudentId}");
            Console.WriteLine($"Report #{selected.PerUserIndex}");
            Console.WriteLine();
            Console.WriteLine(selected.Content);
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the reports list.");
            ReadKeyAllowEscape(intercept: true);
        }
    }

    // Local function to show meetings
    void ShowMeetingsMenu()
    {
        while (true)
        {
            Console.Clear();
            var meetings = ReadMeetings();

            // If logged in user is a student, filter to only meetings that include them
            if (currentUser.Type == UserType.Student)
                meetings = meetings.Where(m => string.Equals(m.BookerId, currentUser.LoginCode, StringComparison.Ordinal)
                                            || string.Equals(m.TargetId, currentUser.LoginCode, StringComparison.Ordinal)).ToList();

            Console.WriteLine("Meetings (Escape to exit)");
            if (currentUser.Type == UserType.Student)
                Console.WriteLine("(showing only meetings that include you)");
            Console.WriteLine();

            if (meetings.Count == 0)
            {
                Console.WriteLine("(no meetings)");
                Console.WriteLine();
                Console.WriteLine("Press any key to return to Database menu.");
                ReadKeyAllowEscape(intercept: true);
                return;
            }

            for (int i = 0; i < meetings.Count; i++)
            {
                var m = meetings[i];
                var date = m.MeetingDate == DateTime.MinValue ? "Unknown date" : m.MeetingDate.ToString("dd/MM/yy");
                var time = m.MeetingDate == DateTime.MinValue ? "Unknown time" : m.MeetingDate.ToString("HH:mm");
                Console.WriteLine($"{i + 1}) {m.BookerName} ({m.BookerId}) -> {m.TargetName} ({m.TargetId}) - {date} {time} - Meeting {m.PerPairIndex}");
            }

            Console.WriteLine();
            Console.Write("Select a meeting number to view or 0 to return: ");
            var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;
            if (!int.TryParse(input, out var selection))
            {
                Console.WriteLine("Invalid selection. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            if (selection == 0)
                return;

            if (selection < 1 || selection > meetings.Count)
            {
                Console.WriteLine("Selection out of range. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            // Show the selected meeting
            var sel = meetings[selection - 1];
            Console.Clear();
            Console.WriteLine("----- MEETING -----");
            Console.WriteLine($"CreatedAt : {sel.CreatedAt:O}");
            Console.WriteLine($"Booker    : {sel.BookerName} ({sel.BookerId})");
            Console.WriteLine($"Target    : {sel.TargetName} ({sel.TargetId})");
            Console.WriteLine($"Date      : {(sel.MeetingDate == DateTime.MinValue ? "Unknown" : sel.MeetingDate.ToString("dd/MM/yy"))}");
            Console.WriteLine($"Time      : {(sel.MeetingDate == DateTime.MinValue ? "Unknown" : sel.MeetingDate.ToString("HH:mm"))}");
            Console.WriteLine($"PairIdx   : {sel.PerPairIndex}");
            Console.WriteLine();
            Console.WriteLine("Reason:");
            Console.WriteLine(sel.Reason);
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the meetings list.");
            ReadKeyAllowEscape(intercept: true);
        }
    }

    // Local function to show student activity (only for supervisors and senior tutors)
    void ShowStudentActivityMenu(Database dbLocal)
    {
        while (true)
        {
            Console.Clear();
            var students = dbLocal.GetAllUsers().Where(u => u.Type == UserType.Student).ToList();
            var reports = ReadReports();
            var meetings = ReadMeetings();

            Console.WriteLine("Student Activity (Escape to exit)");
            Console.WriteLine();

            if (students.Count == 0)
            {
                Console.WriteLine("(no students)");
                Console.WriteLine();
                Console.WriteLine("Press any key to return to Database menu.");
                ReadKeyAllowEscape(intercept: true);
                return;
            }

            for (int i = 0; i < students.Count; i++)
            {
                var s = students[i];
                var reportCount = reports.Count(r => string.Equals(r.StudentId, s.LoginCode, StringComparison.Ordinal));
                var totalMeetings = meetings.Count(m => string.Equals(m.BookerId, s.LoginCode, StringComparison.Ordinal)
                                                     || string.Equals(m.TargetId, s.LoginCode, StringComparison.Ordinal));
                var upcomingBooked = meetings.Count(m => string.Equals(m.BookerId, s.LoginCode, StringComparison.Ordinal)
                                                      && m.MeetingDate >= DateTime.Now);
                Console.WriteLine($"{i + 1}) {s.FirstName} {s.LastName} ({s.LoginCode}) - Reports: {reportCount}, Meetings: {totalMeetings}, UpcomingBooked: {upcomingBooked}");
            }

            Console.WriteLine();
            Console.Write("Select a student number to view details or 0 to return: ");
            var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;
            if (!int.TryParse(input, out var selection))
            {
                Console.WriteLine("Invalid selection. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            if (selection == 0)
                return;

            if (selection < 1 || selection > students.Count)
            {
                Console.WriteLine("Selection out of range. Press any key to continue.");
                ReadKeyAllowEscape(intercept: true);
                continue;
            }

            var student = students[selection - 1];
            var studentReports = reports.Where(r => string.Equals(r.StudentId, student.LoginCode, StringComparison.Ordinal)).ToList();
            var studentMeetings = meetings.Where(m => string.Equals(m.BookerId, student.LoginCode, StringComparison.Ordinal)
                                                  || string.Equals(m.TargetId, student.LoginCode, StringComparison.Ordinal)).ToList();
            var upcomingBookedList = meetings.Where(m => string.Equals(m.BookerId, student.LoginCode, StringComparison.Ordinal)
                                                     && m.MeetingDate >= DateTime.Now)
                                             .OrderBy(m => m.MeetingDate)
                                             .ToList();

            Console.Clear();
            Console.WriteLine($"Student: {student.FirstName} {student.LastName} ({student.LoginCode})");
            Console.WriteLine();
            Console.WriteLine($"Reports filed: {studentReports.Count}");
            Console.WriteLine($"Total meetings (historical): {studentMeetings.Count}");
            Console.WriteLine($"Upcoming meetings they booked: {upcomingBookedList.Count}");
            Console.WriteLine();

            if (studentReports.Count > 0)
            {
                Console.WriteLine("Reports (most recent first):");
                foreach (var r in studentReports.OrderByDescending(r => r.Timestamp).Take(5))
                {
                    var preview = r.Content.Length > 80 ? r.Content.Substring(0, 80) + "…" : r.Content;
                    Console.WriteLine($"- {r.Timestamp:yyyy-MM-dd} #{r.PerUserIndex}: {preview}");
                }

                Console.WriteLine();
            }

            if (upcomingBookedList.Count > 0)
            {
                Console.WriteLine("Upcoming meetings they booked:");
                foreach (var m in upcomingBookedList)
                {
                    Console.WriteLine($"- {m.MeetingDate:dd/MM/yy HH:mm} with {m.TargetName} ({m.TargetId}) - Reason: {m.Reason}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("Press any key to return to the student list.");
            ReadKeyAllowEscape(intercept: true);
        }
    }
}

// New: Tutor Activity Tracker (main-menu entry visible only to Senior Tutors)
static void ShowTutorActivityTracker(Database db, User currentUser)
{
    // defensive guard
    if (currentUser.Type != UserType.SeniorTutor)
    {
        Console.WriteLine("Only Senior Tutors can access the Tutor Activity Tracker.");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to the main menu.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    while (true)
    {
        Console.Clear();
        var allUsers = db.GetAllUsers();
        var supervisors = allUsers.Where(u => u.Type == UserType.PersonalSupervisor).ToList();
        var studentsSet = new HashSet<string>(allUsers.Where(u => u.Type == UserType.Student).Select(u => u.LoginCode), StringComparer.Ordinal);
        var meetings = ReadMeetings();

        Console.WriteLine("Tutor Activity Tracker (Escape to exit)");
        Console.WriteLine();

        if (supervisors.Count == 0)
        {
            Console.WriteLine("(no personal supervisors)");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the main menu.");
            ReadKeyAllowEscape(intercept: true);
            return;
        }

        for (int i = 0; i < supervisors.Count; i++)
        {
            var s = supervisors[i];
            // interactions are any meetings where one side is this supervisor and the other is a student
            var interactions = meetings.Count(m =>
                (string.Equals(m.BookerId, s.LoginCode, StringComparison.Ordinal) && studentsSet.Contains(m.TargetId)) ||
                (string.Equals(m.TargetId, s.LoginCode, StringComparison.Ordinal) && studentsSet.Contains(m.BookerId))
            );
            var upcomingBookedBySupervisor = meetings.Count(m => string.Equals(m.BookerId, s.LoginCode, StringComparison.Ordinal) && m.MeetingDate >= DateTime.Now);
            Console.WriteLine($"{i + 1}) {s.FirstName} {s.LastName} ({s.LoginCode}) - Interactions with students: {interactions}, Upcoming they booked: {upcomingBookedBySupervisor}");
        }

        Console.WriteLine();
        Console.Write("Select a supervisor number to view details or 0 to return: ");
        var input = ReadLineAllowEscape()?.Trim() ?? string.Empty;
        if (!int.TryParse(input, out var sel))
        {
            Console.WriteLine("Invalid selection. Press any key to continue.");
            ReadKeyAllowEscape(intercept: true);
            continue;
        }

        if (sel == 0)
            return;

        if (sel < 1 || sel > supervisors.Count)
        {
            Console.WriteLine("Selection out of range. Press any key to continue.");
            ReadKeyAllowEscape(intercept: true);
            continue;
        }

        var supervisor = supervisors[sel - 1];
        var supervisorMeetings = meetings
            .Where(m => (string.Equals(m.BookerId, supervisor.LoginCode, StringComparison.Ordinal) && studentsSet.Contains(m.TargetId))
                     || (string.Equals(m.TargetId, supervisor.LoginCode, StringComparison.Ordinal) && studentsSet.Contains(m.BookerId)))
            .OrderBy(m => m.MeetingDate)
            .ToList();

        Console.Clear();
        Console.WriteLine($"Personal Supervisor: {supervisor.FirstName} {supervisor.LastName} ({supervisor.LoginCode})");
        Console.WriteLine();

        if (supervisorMeetings.Count == 0)
        {
            Console.WriteLine("(no meetings with students)");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the supervisor list.");
            ReadKeyAllowEscape(intercept: true);
            continue;
        }

        Console.WriteLine("Meetings (chronological):");
        foreach (var m in supervisorMeetings)
        {
            var date = m.MeetingDate == DateTime.MinValue ? "Unknown date" : m.MeetingDate.ToString("dd/MM/yy");
            var time = m.MeetingDate == DateTime.MinValue ? "Unknown time" : m.MeetingDate.ToString("HH:mm");
            Console.WriteLine($"- {date} {time} | Booker: {m.BookerName} ({m.BookerId}) -> Target: {m.TargetName} ({m.TargetId}) | Pair#{m.PerPairIndex}");
            if (!string.IsNullOrEmpty(m.Reason))
                Console.WriteLine($"  Reason: {m.Reason}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to return to the supervisor list.");
        ReadKeyAllowEscape(intercept: true);
    }
}

/// <summary>
/// Reads and parses reports from the student_reports.txt file. Parsing is resilient: entries that do not match expected format are ignored.
/// </summary>
static List<ReportEntry> ReadReports()
{
    var path = GetReportsFilePath();
    var result = new List<ReportEntry>();
    if (!File.Exists(path))
        return result;

    var text = File.ReadAllText(path);
    var parts = text.Split(new[] { "----- REPORT START -----" }, StringSplitOptions.RemoveEmptyEntries);
    var perUserCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var part in parts)
    {
        var trimmed = part.Trim();
        if (string.IsNullOrEmpty(trimmed))
            continue;

        // Extract lines using simple prefix searches
        string GetValue(string prefix)
        {
            var idx = trimmed.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;
            var start = idx + prefix.Length;
            var end = trimmed.IndexOf('\n', start);
            if (end < 0) end = trimmed.Length;
            return trimmed[start..end].Trim();
        }

        var timestampText = GetValue("Timestamp: ");
        var studentName = GetValue("Student: ");
        var studentId = GetValue("Student ID: ");

        // Extract report content between "Report:" and "----- REPORT END -----"
        var reportStartIdx = trimmed.IndexOf("Report:", StringComparison.OrdinalIgnoreCase);
        string content = string.Empty;
        if (reportStartIdx >= 0)
        {
            var contentStart = reportStartIdx + "Report:".Length;
            var endMarkerIdx = trimmed.IndexOf("----- REPORT END -----", contentStart, StringComparison.OrdinalIgnoreCase);
            if (endMarkerIdx >= 0)
            {
                content = trimmed[contentStart..endMarkerIdx].Trim();
            }
            else
            {
                content = trimmed[contentStart..].Trim();
            }
        }

        if (string.IsNullOrEmpty(studentId))
            continue; // skip malformed

        // Parse timestamp
        DateTime timestamp = DateTime.UtcNow;
        if (!DateTime.TryParse(timestampText, null, System.Globalization.DateTimeStyles.RoundtripKind, out timestamp))
        {
            // leave as UtcNow if parsing fails
        }

        // Assign per-user index
        if (!perUserCounts.TryGetValue(studentId, out var count))
            count = 0;
        count++;
        perUserCounts[studentId] = count;

        result.Add(new ReportEntry
        {
            StudentId = studentId,
            StudentName = studentName,
            Timestamp = timestamp,
            Content = content,
            PerUserIndex = count
        });
    }

    return result;
}

static void AddUserFlow(Database db)
{
    Console.Clear();
    Console.WriteLine("Add New User (Escape to exit)");
    Console.WriteLine();
    Console.WriteLine("Select user type:");
    Console.WriteLine("1) Student");
    Console.WriteLine("2) Personal supervisor");
    Console.WriteLine("3) Senior tutor");
    Console.WriteLine();
    Console.Write("Type: ");

    var typeKey = ReadKeyAllowEscape(intercept: true);
    Console.WriteLine();

    UserType type;
    switch (typeKey.KeyChar)
    {
        case '1':
            type = UserType.Student;
            break;
        case '2':
            type = UserType.PersonalSupervisor;
            break;
        case '3':
            type = UserType.SeniorTutor;
            break;
        default:
            Console.WriteLine("Invalid type selection. Press any key to return.");
            ReadKeyAllowEscape(intercept: true);
            return;
    }

    Console.Write("First name: ");
    var firstName = ReadLineAllowEscape()?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(firstName))
    {
        Console.WriteLine("First name cannot be empty. Press any key to return.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    Console.Write("Last name: ");
    var lastName = ReadLineAllowEscape()?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(lastName))
    {
        Console.WriteLine("Last name cannot be empty. Press any key to return.");
        ReadKeyAllowEscape(intercept: true);
        return;
    }

    string loginCode;
    while (true)
    {
        Console.Write("4-digit login code (e.g. 0423): ");
        loginCode = ReadLineAllowEscape()?.Trim() ?? string.Empty;
        if (IsValidFourDigitCode(loginCode))
            break;

        Console.WriteLine("Invalid code. The login code must be exactly 4 digits (0-9). Try again.");
    }

    var user = new User(firstName, lastName, type, loginCode);
    try
    {
        db.AddUser(user);
        Console.WriteLine();
        Console.WriteLine("User added and saved to users.txt.");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Failed to save user: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the Database menu (Escape to exit).");
    ReadKeyAllowEscape(intercept: true);
}

static void ListUsersFlow(Database db)
{
    Console.Clear();
    Console.WriteLine("Users stored in database:");
    Console.WriteLine();

    var users = db.GetAllUsers();
    if (users.Count == 0)
    {
        Console.WriteLine("(no users)");
    }
    else
    {
        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            Console.WriteLine($"{i + 1}. {u.FirstName} {u.LastName} - {u.Type} - {u.LoginCode}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the Database menu (Escape to exit).");
    ReadKeyAllowEscape(intercept: true);
}

static bool IsValidFourDigitCode(string code)
{
    if (code.Length != 4) return false;
    foreach (var c in code)
    {
        if (c < '0' || c > '9') return false;
    }

    return true;
}

sealed class ReportEntry
{
    public string StudentId { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Content { get; init; } = string.Empty;
    public int PerUserIndex { get; init; }
}

sealed class MeetingEntry
{
    public string BookerId { get; init; } = string.Empty;
    public string BookerName { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public DateTime MeetingDate { get; init; }
    public int PerPairIndex { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
}