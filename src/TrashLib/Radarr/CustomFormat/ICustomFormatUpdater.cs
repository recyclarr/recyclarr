using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, RadarrConfiguration config);
}
