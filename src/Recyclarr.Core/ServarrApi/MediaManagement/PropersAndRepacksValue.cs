using System.Text.Json;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.MediaManagement;

// Sonarr and Radarr send the propers mode as a camelCase string. Values a newer server adds read
// as null so they neither fail the sync nor get overwritten unless the user configures a mode.
internal static class PropersAndRepacksValue
{
    public static PropersAndRepacksMode? Parse(string? value)
    {
        return
            Enum.TryParse<PropersAndRepacksMode>(value, ignoreCase: true, out var mode)
            && Enum.IsDefined(mode)
            && !int.TryParse(value, out _)
            ? mode
            : null;
    }

    public static string Format(PropersAndRepacksMode mode)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(mode.ToString());
    }
}
