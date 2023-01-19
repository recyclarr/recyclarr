using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.QualitySize;

public interface IQualitySizeUpdater
{
    Task Process(bool isPreview, QualityDefinitionConfig config, SupportedServices serviceType);
}
