using Content.Management.Domain.SeedWork;

namespace Content.Management.Domain;

/// <summary>The role of an API consumer, driving entity visibility.</summary>
public sealed class UserRole(string key, string name) : Enumeration(key, name)
{
    public static readonly UserRole User = new(nameof(User), nameof(User));

    public static readonly UserRole Admin = new(nameof(Admin), nameof(Admin));

    public static UserRole FromName(string name) =>
        GetAll<UserRole>().SingleOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Possible values for {nameof(UserRole)}: {string.Join(", ", GetAll<UserRole>().Select(r => r.Name))}");
}
