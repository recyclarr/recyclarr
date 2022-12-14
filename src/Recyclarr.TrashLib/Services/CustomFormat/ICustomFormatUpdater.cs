using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Common;

namespace Recyclarr.TrashLib.Services.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, IEnumerable<CustomFormatConfig> configs, IGuideService guideService);
}
