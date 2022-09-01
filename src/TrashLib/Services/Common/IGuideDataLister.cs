using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.Common;

public interface IGuideDataLister
{
    void ListCustomFormats(IEnumerable<CustomFormatData> customFormats);
}
