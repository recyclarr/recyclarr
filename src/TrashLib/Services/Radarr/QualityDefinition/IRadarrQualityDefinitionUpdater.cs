using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.Radarr.QualityDefinition;

public interface IRadarrQualityDefinitionUpdater
{
    Task Process(bool isPreview, RadarrConfiguration config);
}
