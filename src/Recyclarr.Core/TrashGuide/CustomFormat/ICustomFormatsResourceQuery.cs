namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatsResourceQuery
{
    CustomFormatDataResult GetCustomFormatData(SupportedServices serviceType);
}
