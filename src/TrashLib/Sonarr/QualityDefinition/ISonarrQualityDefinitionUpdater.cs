using System.Threading.Tasks;

namespace TrashLib.Sonarr.QualityDefinition
{
    public interface ISonarrQualityDefinitionUpdater
    {
        Task Process(bool isPreview, SonarrConfiguration config);
    }
}
