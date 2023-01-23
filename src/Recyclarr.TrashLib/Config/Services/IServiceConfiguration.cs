namespace Recyclarr.TrashLib.Config.Services;

public interface IServiceConfiguration
{
    SupportedServices ServiceType { get; }
    string? InstanceName { get; }
    Uri BaseUrl { get; }
    string ApiKey { get; }
    bool DeleteOldCustomFormats { get; }
    ICollection<CustomFormatConfig> CustomFormats { get; }
    QualityDefinitionConfig? QualityDefinition { get; }

    IReadOnlyCollection<QualityProfileConfig> QualityProfiles { get; }
    bool ReplaceExistingCustomFormats { get; init; }
}
