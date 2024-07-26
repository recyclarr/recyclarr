using System.IO.Abstractions;
using Autofac.Features.Indexed;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class YamlIncludeResolver(IIndex<Type, IIncludeProcessor> processorFactory) : IYamlIncludeResolver
{
    public IFileInfo GetIncludePath(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        if (!processorFactory.TryGetValue(includeDirective.GetType(), out var processor))
        {
            throw new YamlIncludeException("Include type is not supported");
        }

        var yamlFile = processor.GetPathToConfig(includeDirective, serviceType);
        if (!yamlFile.Exists)
        {
            throw new YamlIncludeException($"Included YAML file does not exist: {yamlFile.FullName}");
        }

        return yamlFile;
    }
}
