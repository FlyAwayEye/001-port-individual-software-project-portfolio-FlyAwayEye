using System;
using System.IO;
using System.Linq;
using Xunit;

public class IncorrectInputTests : IDisposable
{
    private readonly string _usersPath;

    public IncorrectInputTests()
    {
        // Database uses AppContext.BaseDirectory for storage; tests run in the test runner directory.
        _usersPath = Path.Combine(AppContext.BaseDirectory ?? ".", "users.txt");

        // Ensure clean state
        if (File.Exists(_usersPath))
            File.Delete(_usersPath);
    }

    [Fact]
    public void Initialize_CreatesDefaultUsers_WhenFileMissing()
    {
        var db = new Database();
        db.Initialize();

        Assert.True(File.Exists(_usersPath), "users.txt should be created");
        var users = db.GetAllUsers();
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Type == UserType.Student && u.LoginCode == "0001");
        Assert.Contains(users, u => u.Type == UserType.PersonalSupervisor && u.LoginCode == "0002");
        Assert.Contains(users, u => u.Type == UserType.SeniorTutor && u.LoginCode == "0003");
    }

    [Fact]
    public void Initialize_CreatesDefaults_WhenFileContainsInvalidJson()
    {
        // write invalid json to simulate corruption
        File.WriteAllText(_usersPath, "{ this is not valid json }");

        var db = new Database();
        db.Initialize(); // should treat file as invalid and create defaults

        var users = db.GetAllUsers();
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Type == UserType.Student && u.LoginCode == "0001");
    }

    [Fact]
    public void AddUser_Throws_OnNull()
    {
        var db = new Database();
        db.Initialize();

        Assert.Throws<ArgumentNullException>(() => db.AddUser(null!));
    }

    [Fact]
    public void SaveUsers_RewritesFile_AndMaintainsSortOrder()
    {
        var db = new Database();
        db.Initialize();

        var items = new[]
        {
            User.CreateSeniorTutor("S","Z","9001"),
            User.CreateStudent("A","A","1001"),
            User.CreatePersonalSupervisor("P","P","2001")
        };

        db.SaveUsers(items);

        var all = db.GetAllUsers().ToList();
        Assert.Equal(UserType.Student, all[0].Type);
        Assert.Equal(UserType.PersonalSupervisor, all[1].Type);
        Assert.Equal(UserType.SeniorTutor, all[2].Type);
    }

    [Theory]
    [InlineData("123")]      // too short
    [InlineData("12345")]    // too long
    [InlineData("12a4")]     // non-digit
    [InlineData("")]         // empty
    public void FourDigitFormat_IsInvalid(string code)
    {
        bool IsValidFourDigitCode(string c)
        {
            if (string.IsNullOrEmpty(c)) return false;
            if (c.Length != 4) return false;
            foreach (var ch in c)
                if (ch < '0' || ch > '9') return false;
            return true;
        }

        Assert.False(IsValidFourDigitCode(code));
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_usersPath))
                File.Delete(_usersPath);
        }
        catch { /* best-effort cleanup */ }
    }
}