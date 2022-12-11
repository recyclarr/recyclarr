using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.QualitySize;

namespace TrashLib.Services.Common;

public interface IGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
    ICollection<QualitySizeData> GetQualities();
}
