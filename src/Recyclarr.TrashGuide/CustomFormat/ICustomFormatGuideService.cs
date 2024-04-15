namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
