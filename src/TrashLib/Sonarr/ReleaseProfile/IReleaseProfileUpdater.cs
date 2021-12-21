using System.Threading.Tasks;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile;

public interface IReleaseProfileUpdater
{
    Task Process(bool isPreview, SonarrConfiguration config);
}
