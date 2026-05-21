namespace Recyclarr.Server;

/// <summary>
/// Parses well-known command-line arguments for the server process.
/// </summary>
internal static class ServerArgsParser
{
    private const string ParentPidPrefix = "--parent-pid=";

    /// <summary>
    /// Returns the value of <c>--parent-pid={pid}</c> from <paramref name="args"/>, or
    /// <see langword="null"/> if the flag is absent or the value is not a valid integer.
    /// </summary>
    public static int? ParseParentPid(string[] args)
    {
        foreach (var arg in args)
        {
            if (
                arg.StartsWith(ParentPidPrefix, StringComparison.Ordinal)
                && int.TryParse(arg[ParentPidPrefix.Length..], out var pid)
            )
            {
                return pid;
            }
        }

        return null;
    }
}
