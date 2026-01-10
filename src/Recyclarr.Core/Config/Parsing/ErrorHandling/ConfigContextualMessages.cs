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

        if (
            msg.Contains(
                "Property 'reset_unmatched_scores' not found on type "
                    + $"'{typeof(QualityScoreConfigYaml).FullName}'",
                StringComparison.Ordinal
            )
        )
        {
            return "Usage of 'reset_unmatched_scores' inside 'quality_profiles' under 'custom_formats' is no "
                + "longer supported. Use the root-level 'quality_profiles' instead. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v5.0/#reset-unmatched-scores>";
        }

        if (
            msg.Contains(
                $"Invalid cast from 'System.String' to '{typeof(ResetUnmatchedScoresConfigYaml).FullName}'",
                StringComparison.Ordinal
            )
        )
        {
            return "Using true/false with `reset_unmatched_scores` is no longer supported. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v6.0/#reset-scores>";
        }

        if (msg.Contains("Property 'release_profiles' not found on type", StringComparison.Ordinal))
        {
            return "Release profiles and Sonarr v3 in general are no longer supported. All instances of "
                + "`release_profiles` in your configuration YAML must be removed. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v7.0/#sonarr-removed>";
        }

        if (
            msg.Contains(
                "Property 'replace_existing_custom_formats' not found on type",
                StringComparison.Ordinal
            )
        )
        {
            return "The `replace_existing_custom_formats` option has been removed. "
                + "Use `recyclarr cache rebuild --adopt` to adopt manually-created custom formats. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#replace-existing-removed>";
        }

        return null;
    }
}
