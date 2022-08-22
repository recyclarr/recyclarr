namespace TrashLib.Services.Radarr.Config;

public interface IRadarrValidationMessages
{
    string BaseUrl { get; }
    string ApiKey { get; }
    string CustomFormatNamesAndIds { get; }
    string QualityProfileName { get; }
    string QualityDefinitionType { get; }
}
