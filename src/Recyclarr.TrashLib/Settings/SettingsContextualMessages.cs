using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Settings;

public static class SettingsContextualMessages
{
    public static string? GetContextualErrorFromException(YamlException e)
    {
        if (e.Message.Contains(
            "Property 'repository' not found on type " +
            $"'{typeof(SettingsValues).FullName}'"))
        {
            return
                "Usage of 'repository' setting is no " +
                "longer supported. Use 'trash_guides' under 'repositories' instead." +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v5.0/#settings-repository-changes";
        }

        return null;
    }
}
