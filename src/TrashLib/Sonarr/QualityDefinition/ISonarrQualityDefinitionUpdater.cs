using System.Threading.Tasks;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.QualityDefinition
{
    public interface ISonarrQualityDefinitionUpdater
    {
        Task Process(bool isPreview, SonarrConfiguration config);
    }
}
