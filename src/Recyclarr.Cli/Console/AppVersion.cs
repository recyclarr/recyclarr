using System.Reflection;

namespace Recyclarr.Cli.Console;

/// <summary>
/// The version stamped into the assembly at build time: GitVersion computes it in a normal build,
/// and the Docker image build passes it in as a build argument because the image has no Git
/// repository (see Dockerfile).
/// </summary>
internal static class AppVersion
{
    /// <summary>
    /// GitVersion's informational version, for example
    /// <c>9.0.0-alpha.1+12.Branch.master.Sha.abc123</c>.
    /// </summary>
    public static string Informational { get; } =
        typeof(AppVersion)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";

    /// <summary>
    /// The semantic version with its build metadata in parentheses, as shown by <c>--version</c>.
    /// </summary>
    public static string Display { get; } = FormatDisplay(Informational);

    private static string FormatDisplay(string informational)
    {
        var separator = informational.IndexOf('+', StringComparison.Ordinal);
        return separator < 0
            ? $"v{informational}"
            : $"v{informational[..separator]} ({informational[(separator + 1)..]})";
    }
}
