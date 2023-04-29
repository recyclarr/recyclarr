using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Config.Parsing;

public static class ConfigDeprecations
{
    public static string? GetContextualErrorFromException(YamlException e)
    {
        if (e.Message.Contains("Expected 'MappingStart', got 'SequenceStart'"))
        {
            return "Found array-style list of instances instead of named-style. " +
                "Array-style lists of Sonarr/Radarr instances are not supported. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v5.0/#instances-must-now-be-named";
        }

        return null;
    }
}
