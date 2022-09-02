using TrashLib.Config.Services;
using TrashLib.Services.Common;

namespace TrashLib.Services.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, IEnumerable<CustomFormatConfig> config, IGuideService guideService);
}
