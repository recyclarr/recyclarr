using JetBrains.Annotations;
using Recyclarr.TrashLib.Services.Common;

namespace Recyclarr.TrashLib.Services.Radarr;

[UsedImplicitly]
public class RadarrGuideDataLister : IRadarrGuideDataLister
{
    private readonly RadarrGuideService _guide;
    private readonly IGuideDataLister _guideLister;

    public RadarrGuideDataLister(
        RadarrGuideService guide,
        IGuideDataLister guideLister)
    {
        _guide = guide;
        _guideLister = guideLister;
    }

    public void ListCustomFormats()
    {
        _guideLister.ListCustomFormats(_guide.GetCustomFormatData());
    }

    public void ListQualities()
    {
        _guideLister.ListQualities(_guide.GetQualities());
    }
}
