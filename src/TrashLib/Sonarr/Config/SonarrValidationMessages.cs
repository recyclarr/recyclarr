using JetBrains.Annotations;

namespace TrashLib.Sonarr.Config
{
    [UsedImplicitly]
    internal class SonarrValidationMessages : ISonarrValidationMessages
    {
        public string BaseUrl =>
            "Property 'base_url' is required";

        public string ApiKey =>
            "Property 'api_key' is required";

        public string ReleaseProfileType =>
            "'type' is required for 'release_profiles' elements";
    }
}
