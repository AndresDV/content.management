using System.Reflection;

namespace Content.Management.Domain.SeedWork;

/// <summary>
/// Base class for strongly-typed enumerations (a thin wrapper around a string key
/// and a display name).
/// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types
/// </summary>
public abstract class Enumeration(string key, string name) : IComparable
{
    public string Name { get; } = name;

    public string Key { get; } = key;

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue)
        {
            return false;
        }

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Key.Equals(otherValue.Key);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => Key.GetHashCode();

    public static T FromKey<T>(string key) where T : Enumeration =>
        Parse<T, string>(key, "key", item => item.Key == key);

    public static T FromName<T>(string name) where T : Enumeration =>
        Parse<T, string>(name, "name", item => item.Name == name);

    private static T Parse<T, TValue>(TValue value, string description, Func<T, bool> predicate)
        where T : Enumeration =>
        GetAll<T>().FirstOrDefault(predicate)
        ?? throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

    public int CompareTo(object? obj) =>
        obj is null
            ? throw new ArgumentNullException(nameof(obj))
            : string.Compare(Key, ((Enumeration)obj).Key, StringComparison.Ordinal);
}
