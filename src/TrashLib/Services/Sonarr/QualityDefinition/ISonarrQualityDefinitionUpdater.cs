using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr.QualityDefinition;

public interface ISonarrQualityDefinitionUpdater
{
    Task Process(bool isPreview, SonarrConfiguration config);
}
