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

        if (msg.Contains(
            "Property 'repository' not found on type " +
            $"'{typeof(RecyclarrSettings).FullName}'"))
        {
            return
                "Usage of 'repository' setting is no " +
                "longer supported. Use 'trash_guides' under 'repositories' instead." +
                "See: <https://recyclarr.dev/wiki/upgrade-guide/v5.0/#settings-repository-changes>";
        }

        return null;
    }
}
