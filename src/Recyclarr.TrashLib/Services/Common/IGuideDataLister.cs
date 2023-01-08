using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;

namespace Recyclarr.TrashLib.Services.Common;

public interface IGuideDataLister
{
    void ListCustomFormats(IEnumerable<CustomFormatData> customFormats);
    void ListQualities(IEnumerable<QualitySizeData> qualityData);
}
