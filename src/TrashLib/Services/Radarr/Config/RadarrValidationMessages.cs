namespace TrashLib.Services.Radarr.Config;

internal class RadarrValidationMessages : IRadarrValidationMessages
{
    public string BaseUrl =>
        "Property 'base_url' is required";

    public string ApiKey =>
        "Property 'api_key' is required";

    public string CustomFormatTrashIds =>
        "'custom_formats' elements must contain at least one element under 'trash_ids'";

    public string QualityProfileName =>
        "'name' is required for elements under 'quality_profiles'";

    public string QualityDefinitionType =>
        "'type' is required for 'quality_definition'";
}
