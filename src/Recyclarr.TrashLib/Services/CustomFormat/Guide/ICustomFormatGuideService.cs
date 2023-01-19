using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
