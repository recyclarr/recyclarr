using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public interface ICustomFormatGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
