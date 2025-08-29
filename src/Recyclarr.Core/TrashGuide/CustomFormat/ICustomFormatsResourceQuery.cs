namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatsResourceQuery
{
    ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType);
}
