using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

public interface ICustomFormatGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
