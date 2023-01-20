using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.CustomFormat;

public interface ICustomFormatUpdater
{
    Task Process(bool isPreview, IServiceConfiguration config);
}
