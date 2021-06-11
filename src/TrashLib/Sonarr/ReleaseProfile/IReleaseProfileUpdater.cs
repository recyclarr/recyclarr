using System.Threading.Tasks;

namespace TrashLib.Sonarr.ReleaseProfile
{
    public interface IReleaseProfileUpdater
    {
        Task Process(bool isPreview, SonarrConfiguration config);
    }
}
