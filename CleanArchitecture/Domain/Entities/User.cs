namespace PicoPlus.CleanArchitecture.Domain.Entities;

public sealed class User
{
    public string Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? PhoneNumber { get; }
    public string Role { get; }

    public User(string id, string firstName, string lastName, string? phoneNumber, string role)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Role = role;
    }
}
