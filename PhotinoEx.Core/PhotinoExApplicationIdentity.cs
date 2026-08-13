using System.Reflection;
using System.Text.RegularExpressions;

namespace PhotinoEx.Core;

public static partial class PhotinoExApplicationIdentity
{
    public static string DefaultId => Normalize(
        Assembly.GetEntryAssembly()?.GetName().Name ?? "PhotinoEx");

    public static string Normalize(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        var normalized = applicationId.Trim().ToLowerInvariant();
        if (!ValidId().IsMatch(normalized))
        {
            throw new ArgumentException(
                "The application ID must start with a letter and contain only letters, digits, dots, or hyphens.",
                nameof(applicationId));
        }

        return normalized;
    }

    public static string GetWindowsDataPath(string applicationId) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhotinoEx",
            Normalize(applicationId));

    public static string GetLinuxApplicationId(string applicationId)
    {
        var id = Normalize(applicationId);
        return id.Contains('.') ? id : $"com.photinoex.{id}";
    }

    [GeneratedRegex("^[a-z][a-z0-9.-]*$")]
    private static partial Regex ValidId();
}
