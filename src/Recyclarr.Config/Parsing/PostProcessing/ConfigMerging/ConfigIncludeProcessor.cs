using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Platform;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class ConfigIncludeProcessor : IIncludeProcessor
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;

    public ConfigIncludeProcessor(IFileSystem fs, IAppPaths paths)
    {
        _fs = fs;
        _paths = paths;
    }

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

        var rooted = _fs.Path.IsPathRooted(include.Config);

        var configFile = rooted
            ? _fs.FileInfo.New(include.Config)
            : _paths.ConfigsDirectory.File(include.Config);

        if (!configFile.Exists)
        {
            var pathType = rooted ? "Absolute" : "Relative";
            throw new YamlIncludeException($"{pathType} include path does not exist: {configFile.FullName}");
        }

        return configFile;
    }
}
