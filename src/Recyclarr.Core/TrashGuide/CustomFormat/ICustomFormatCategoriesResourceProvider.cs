using Recyclarr.ResourceProviders;

namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatCategoriesResourceProvider : IResourceProvider
{
    ICollection<CustomFormatCategoryItem> GetCategoryData();
}
