using System.Threading.Tasks;
using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.QualityDefinition
{
    public interface IRadarrQualityDefinitionUpdater
    {
        Task Process(bool isPreview, RadarrConfig config);
    }
}
