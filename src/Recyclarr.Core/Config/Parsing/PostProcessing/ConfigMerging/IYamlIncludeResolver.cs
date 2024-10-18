using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public interface IYamlIncludeResolver
{
    IFileInfo GetIncludePath(IYamlInclude includeDirective, SupportedServices serviceType);
}
