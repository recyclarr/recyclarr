using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;

public interface ICustomFormatGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
