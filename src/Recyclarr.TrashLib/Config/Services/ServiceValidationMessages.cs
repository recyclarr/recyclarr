namespace Recyclarr.TrashLib.Config.Services;

internal /*abstract*/ class ServiceValidationMessages : IServiceValidationMessages
{
    public string BaseUrl =>
        "Property 'base_url' is required";

    public string ApiKey =>
        "Property 'api_key' is required";

    public string CustomFormatTrashIds =>
        "'custom_formats' elements must contain at least one element under 'trash_ids'";
}
