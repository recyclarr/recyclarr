using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, IEnumerable<CustomFormatConfig> config);
}
