using YamlDotNet.Core;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public static class ConfigContextualMessages
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

        if (msg.Contains("Property 'quality_profiles' not found on type", StringComparison.Ordinal))
        {
            return "The `quality_profiles` element under `custom_formats` has been renamed to `assign_scores_to`. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#assign-scores-to>";
        }

        return null;
    }
}
