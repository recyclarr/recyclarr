using JetBrains.Annotations;

namespace TrashLib.Services.Sonarr.Config;

[UsedImplicitly]
internal class SonarrValidationMessages : ISonarrValidationMessages
{
    public string BaseUrl =>
        "Property 'base_url' is required";

    public string ApiKey =>
        "Property 'api_key' is required";

    public string ReleaseProfileTrashIds =>
        "'trash_ids' is required for 'release_profiles' elements";
}
