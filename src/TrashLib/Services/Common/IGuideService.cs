using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.Common;

public interface IGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
}
