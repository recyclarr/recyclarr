using System.IO.Abstractions;
using Recyclarr.Common;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class YamlIncludeResolver : IYamlIncludeResolver
{
    private readonly IReadOnlyCollection<IIncludeProcessor> _includeProcessors;

    public YamlIncludeResolver(IReadOnlyCollection<IIncludeProcessor> includeProcessors)
    {
        _includeProcessors = includeProcessors;
    }

    public IFileInfo GetIncludePath(IYamlInclude includeType, SupportedServices serviceType)
    {
        var processor = _includeProcessors.FirstOrDefault(x => x.CanProcess(includeType));
        if (processor is null)
        {
            throw new YamlIncludeException("Include type is not supported");
        }

        var yamlFile = processor.GetPathToConfig(includeType, serviceType);
        if (!yamlFile.Exists)
        {
            throw new YamlIncludeException($"Included YAML file does not exist: {yamlFile.FullName}");
        }

        return yamlFile;
    }
}
