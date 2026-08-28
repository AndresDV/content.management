namespace Content.Management.Application.Extensions;

/// <summary>Extension helpers for readable generic type names in logs.</summary>
public static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this object source) => source.GetType().GetGenericTypeName();

    public static string GetGenericTypeName(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
        return $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
    }
}
