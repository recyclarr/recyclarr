using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.Common;

public interface IGuideDataLister
{
    void ListCustomFormats(IEnumerable<CustomFormatData> customFormats);
}
