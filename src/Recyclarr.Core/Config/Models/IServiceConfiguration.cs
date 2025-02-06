using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Models;

public interface IServiceConfiguration
{
    IFileInfo? YamlPath { get; }
    SupportedServices ServiceType { get; }
    string InstanceName { get; }
    Uri BaseUrl { get; }
    string ApiKey { get; }
    bool DeleteOldCustomFormats { get; }
    ICollection<CustomFormatConfig> CustomFormats { get; }
    QualityDefinitionConfig? QualityDefinition { get; }
    IReadOnlyCollection<QualityProfileConfig> QualityProfiles { get; }
    bool ReplaceExistingCustomFormats { get; }
}
