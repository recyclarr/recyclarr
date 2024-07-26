using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public interface IIncludeProcessor
{
    IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType);
}
