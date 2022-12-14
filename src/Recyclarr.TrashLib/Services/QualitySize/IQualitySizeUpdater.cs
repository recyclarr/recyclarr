using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Common;

namespace Recyclarr.TrashLib.Services.QualitySize;

public interface IQualitySizeUpdater
{
    Task Process(bool isPreview, QualityDefinitionConfig config, IGuideService guideService);
}
