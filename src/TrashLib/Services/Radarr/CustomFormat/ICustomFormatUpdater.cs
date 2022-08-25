using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.Radarr.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, RadarrConfiguration config);
}
