using JetBrains.Annotations;

namespace TrashLib.Services.Sonarr.Config;

[UsedImplicitly]
internal class SonarrValidationMessages : ISonarrValidationMessages
{
    public string ReleaseProfileTrashIds =>
        "'trash_ids' is required for 'release_profiles' elements";
}
