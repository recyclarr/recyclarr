using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public interface IIncludeProcessor
{
    bool CanProcess(IYamlInclude includeDirective);
    IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType);
}
