namespace Recyclarr.Server;

/// <summary>
/// Parses well-known command-line arguments for the server process.
/// CLI args take precedence over settings.yml values.
/// </summary>
internal static class ServerArgsParser
{
    private const string ParentPidPrefix = "--parent-pid=";
    private const string PortPrefix = "--port=";
    private const string BindAddressPrefix = "--bind-address=";

    internal record ParsedArgs(int? ParentPid, int? Port, string? BindAddress);

    public static ParsedArgs Parse(string[] args)
    {
        int? parentPid = null;
        int? port = null;
        string? bindAddress = null;

        foreach (var arg in args)
        {
            if (
                arg.StartsWith(ParentPidPrefix, StringComparison.Ordinal)
                && int.TryParse(arg[ParentPidPrefix.Length..], out var pid)
            )
            {
                parentPid = pid;
            }
            else if (
                arg.StartsWith(PortPrefix, StringComparison.Ordinal)
                && int.TryParse(arg[PortPrefix.Length..], out var p)
            )
            {
                port = p;
            }
            else if (arg.StartsWith(BindAddressPrefix, StringComparison.Ordinal))
            {
                bindAddress = arg[BindAddressPrefix.Length..];
            }
        }

        return new ParsedArgs(parentPid, port, bindAddress);
    }
}
