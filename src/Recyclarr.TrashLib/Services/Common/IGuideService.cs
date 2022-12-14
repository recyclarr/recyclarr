using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;

namespace Recyclarr.TrashLib.Services.Common;

public interface IGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
    ICollection<QualitySizeData> GetQualities();
}
