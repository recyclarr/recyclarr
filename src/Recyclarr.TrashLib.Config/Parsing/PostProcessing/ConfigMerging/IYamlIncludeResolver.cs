using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

public interface IYamlIncludeResolver
{
    IFileInfo GetIncludePath(IYamlInclude includeType, SupportedServices serviceType);
}
