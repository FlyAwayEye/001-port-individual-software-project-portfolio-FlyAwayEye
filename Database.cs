using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed class Database
{
    private readonly object _fileLock = new();
    private const string UsersFileName = "users.txt";

    public Database()
    {
    }

    /// <summary>
    /// Initializes the on-disk storage if required. Currently ensures the users file exists.
    /// The files are stored in the application's base directory (usually bin/.../net8.0 at runtime).
    /// On first run (no users present) three default accounts are created to avoid lockout.
    /// </summary>
    public void Initialize()
    {
        lock (_fileLock)
        {
            // Load existing users (returns empty list if file missing/invalid)
            var users = LoadUsersInternal();

            if (users.Count == 0)
            {
                // Create three default users to avoid lockout on first run.
                var defaultUsers = new List<User>
                {
                    User.CreateStudent("Alice", "Student", "0001"),
                    User.CreatePersonalSupervisor("Paul", "Supervisor", "0002"),
                    User.CreateSeniorTutor("Sam", "Senior", "0003")
                };

                // Ensure the same sort order used elsewhere.
                SortUsers(defaultUsers);
                SaveUsersInternal(defaultUsers);
            }
        }
    }

    /// <summary>
    /// Adds a user, sorts the collection and persists it to disk.
    /// </summary>
    public void AddUser(User user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        lock (_fileLock)
        {
            var users = LoadUsersInternal();
            users.Add(user);
            SortUsers(users);
            SaveUsersInternal(users);
        }
    }

    /// <summary>
    /// Returns all users currently stored on disk. If the file does not exist an empty list is returned.
    /// </summary>
    public List<User> GetAllUsers()
    {
        lock (_fileLock)
        {
            return LoadUsersInternal();
        }
    }

    /// <summary>
    /// Persists the provided users to disk (replacing existing users file).
    /// Users will be sorted before saving.
    /// </summary>
    public void SaveUsers(IEnumerable<User> users)
    {
        if (users is null) throw new ArgumentNullException(nameof(users));

        lock (_fileLock)
        {
            var list = users.ToList();
            SortUsers(list);
            SaveUsersInternal(list);
        }
    }

    /// <summary>
    /// Sorts the provided list of users in-place using the following precedence:
    /// 1) UserType (enum order: Student, PersonalSupervisor, SeniorTutor)
    /// 2) LastName (ordinal, case-insensitive)
    /// 3) FirstName (ordinal, case-insensitive)
    /// </summary>
    private static void SortUsers(List<User> users)
    {
        users.Sort((a, b) =>
        {
            var byType = a.Type.CompareTo(b.Type);
            if (byType != 0) return byType;

            var last = string.Compare(a.LastName ?? string.Empty, b.LastName ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
            if (last != 0) return last;

            return string.Compare(a.FirstName ?? string.Empty, b.FirstName ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Loads users from disk. Returns an empty list if the file doesn't exist or is invalid.
    /// </summary>
    private List<User> LoadUsersInternal()
    {
        var path = GetFilePath(UsersFileName);
        try
        {
            if (!File.Exists(path))
                return new List<User>();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new List<User>();

            var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

            var users = JsonSerializer.Deserialize<List<User>>(json, opts);
            return users ?? new List<User>();
        }
        catch
        {
            // On any error return an empty list so caller can continue working.
            return new List<User>();
        }
    }

    /// <summary>
    /// Saves the given users list to disk as a JSON array.
    /// </summary>
    private void SaveUsersInternal(List<User> users)
    {
        var path = GetFilePath(UsersFileName);
        var opts = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(users, opts);
        File.WriteAllText(path, json);
    }

    private static string GetFilePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory ?? ".", fileName);

    public override string ToString() => "Database (file-backed, users.txt)";
}