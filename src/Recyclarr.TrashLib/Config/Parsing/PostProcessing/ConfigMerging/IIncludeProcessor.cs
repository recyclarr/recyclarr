using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

public interface IIncludeProcessor
{
    bool CanProcess(IYamlInclude includeDirective);
    IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType);
}
