using System.Threading.Tasks;

namespace TrashLib.Radarr.QualityDefinition
{
    public interface IRadarrQualityDefinitionUpdater
    {
        Task Process(bool isPreview, RadarrConfiguration config);
    }
}
