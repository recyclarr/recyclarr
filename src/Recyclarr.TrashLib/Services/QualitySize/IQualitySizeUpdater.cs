using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.QualitySize;

public interface IQualitySizeUpdater
{
    Task Process(bool isPreview, IServiceConfiguration config);
}
