using System.IO.Abstractions;
using Recyclarr.Common;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public interface IYamlIncludeResolver
{
    IFileInfo GetIncludePath(IYamlInclude includeType, SupportedServices serviceType);
}
