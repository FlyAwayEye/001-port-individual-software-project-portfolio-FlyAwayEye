using System;

public enum UserType
{
    Student,
    PersonalSupervisor,
    SeniorTutor
}

public class User
{
    public Guid Id { get; init; }

    // New discrete name properties
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Preserve compatibility: computed FullName that can be set (splits into first/last)
    public string FullName
    {
        get => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? string.Empty
            : $"{FirstName} {LastName}";
        set
        {
            if (value is null)
            {
                FirstName = LastName = string.Empty;
                return;
            }

            var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            FirstName = parts.Length > 0 ? parts[0] : string.Empty;
            LastName = parts.Length > 1 ? parts[1] : string.Empty;
        }
    }

    // New 4-digit login code holder (string to preserve leading zeros)
    public string LoginCode { get; set; } = string.Empty;

    public User()
    {
        Id = Guid.NewGuid();
    }

    // Backward-compatible constructor that accepts a full name
    public User(string fullName, UserType type) : this()
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Type = type;
    }

    // New constructor that accepts first and last name and optional login code
    public User(string firstName, string lastName, UserType type, string loginCode = "") : this()
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Type = type;
        LoginCode = loginCode ?? string.Empty;
    }

    public UserType Type { get; set; }

    // Existing factory methods kept for compatibility
    public static User CreateStudent(string fullName) =>
        new(fullName, UserType.Student);

    public static User CreatePersonalSupervisor(string fullName) =>
        new(fullName, UserType.PersonalSupervisor);

    public static User CreateSeniorTutor(string fullName) =>
        new(fullName, UserType.SeniorTutor);

    // New factory overloads that accept discrete names and optional login code
    public static User CreateStudent(string firstName, string lastName, string loginCode = "") =>
        new(firstName, lastName, UserType.Student, loginCode);

    public static User CreatePersonalSupervisor(string firstName, string lastName, string loginCode = "") =>
        new(firstName, lastName, UserType.PersonalSupervisor, loginCode);

    public static User CreateSeniorTutor(string firstName, string lastName, string loginCode = "") =>
        new(firstName, lastName, UserType.SeniorTutor, loginCode);

    public override string ToString() => $"{FullName} ({Type})";
}