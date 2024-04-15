using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class ConfigIncludeProcessor(IFileSystem fs, IAppPaths paths) : IIncludeProcessor
{
    public bool CanProcess(IYamlInclude includeDirective)
    {
        return includeDirective is ConfigYamlInclude;
    }

    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (ConfigYamlInclude) includeDirective;

        if (include.Config is null)
        {
            throw new YamlIncludeException("`config` property is required.");
        }

        var rooted = fs.Path.IsPathRooted(include.Config);

        var configFile = rooted
            ? fs.FileInfo.New(include.Config)
            : paths.ConfigsDirectory.File(include.Config);

        if (!configFile.Exists)
        {
            var pathType = rooted ? "Absolute" : "Relative";
            throw new YamlIncludeException($"{pathType} include path does not exist: {configFile.FullName}");
        }

        return configFile;
    }
}
