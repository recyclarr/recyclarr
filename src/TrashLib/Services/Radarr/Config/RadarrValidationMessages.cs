namespace TrashLib.Services.Radarr.Config;

internal class RadarrValidationMessages : IRadarrValidationMessages
{
    public string QualityProfileName =>
        "'name' is required for elements under 'quality_profiles'";

    public string QualityDefinitionType =>
        "'type' is required for 'quality_definition'";
}
