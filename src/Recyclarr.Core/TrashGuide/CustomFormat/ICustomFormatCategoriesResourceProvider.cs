using System.IO.Abstractions;
using Recyclarr.ResourceProviders;

namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatCategoriesResourceProvider : IResourceProvider
{
    IFileInfo? GetCategoryMarkdownFile(SupportedServices serviceType);
}
