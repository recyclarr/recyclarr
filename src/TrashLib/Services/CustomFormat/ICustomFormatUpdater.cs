using TrashLib.Config.Services;

namespace TrashLib.Services.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, IEnumerable<CustomFormatConfig> config);
}
