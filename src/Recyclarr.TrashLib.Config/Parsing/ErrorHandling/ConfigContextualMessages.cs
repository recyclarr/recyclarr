using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Config.Parsing.ErrorHandling;

public static class ConfigContextualMessages
{
    public static string? GetContextualErrorFromException(YamlException e)
    {
        if (e.Message.Contains(
                "Property 'reset_unmatched_scores' not found on type " +
                $"'{typeof(QualityScoreConfigYaml).FullName}'"))
        {
            return
                "Usage of 'reset_unmatched_scores' inside 'quality_profiles' under 'custom_formats' is no " +
                "longer supported. Use the root-level 'quality_profiles' instead. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v5.0/#reset-unmatched-scores";
        }

        return null;
    }
}
