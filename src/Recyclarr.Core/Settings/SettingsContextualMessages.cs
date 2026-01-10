using Recyclarr.Settings.Models;
using YamlDotNet.Core;

namespace Recyclarr.Settings;

public static class SettingsContextualMessages
{
    public static string? GetContextualErrorFromException(YamlException e)
    {
        return LookupMessage(e.Message) ?? LookupMessage(e.InnerException?.Message);
    }

    private static string? LookupMessage(string? msg)
    {
        if (msg is null)
        {
            return null;
        }

        if (
            msg.Contains(
                $"Property 'repositories' not found on type '{typeof(RecyclarrSettings).FullName}'",
                StringComparison.Ordinal
            )
        )
        {
            return "The 'repositories' setting has been removed. "
                + "Use 'resource_providers' instead. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers>";
        }

        if (
            msg.Contains(
                $"Property 'repository' not found on type '{typeof(RecyclarrSettings).FullName}'",
                StringComparison.Ordinal
            )
        )
        {
            return "The 'repository' setting has been removed. "
                + "Use 'resource_providers' instead. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers>";
        }

        return null;
    }
}
