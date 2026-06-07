using Recyclarr.Settings.Models;

namespace Recyclarr.Cli;

/// <summary>
/// Builds a <c>--urls</c> value for the server process from YAML settings and optional CLI
/// overrides. CLI values take precedence over YAML, which falls back to <see cref="ServerSettings"/>
/// defaults.
/// </summary>
internal static class ServerUrlBuilder
{
    public static string Build(
        ServerSettings settings,
        int? portOverride = null,
        string? bindAddressOverride = null
    )
    {
        var port = portOverride ?? settings.Port;
        var bindAddress = bindAddressOverride ?? settings.BindAddress;
        return $"http://{bindAddress}:{port}";
    }
}
