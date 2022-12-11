using TrashLib.Config.Services;
using TrashLib.Services.Common;

namespace TrashLib.Services.QualitySize;

public interface IQualitySizeUpdater
{
    Task Process(bool isPreview, QualityDefinitionConfig config, IGuideService guideService);
}
