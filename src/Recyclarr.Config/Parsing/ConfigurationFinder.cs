using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Platform;

namespace Recyclarr.Config.Parsing;

public class ConfigurationFinder : IConfigurationFinder
{
    private readonly IAppPaths _paths;

    public ConfigurationFinder(IAppPaths paths)
    {
        _paths = paths;
    }

    private IReadOnlyCollection<IFileInfo> FindDefaultConfigFiles()
    {
        var configs = new List<IFileInfo>();

        if (_paths.ConfigsDirectory.Exists)
        {
            var extensions = new[] {"*.yml", "*.yaml"};
            var files = extensions.SelectMany(x => _paths.ConfigsDirectory.EnumerateFiles(x));
            configs.AddRange(files);
        }

        var configPath = _paths.AppDataDirectory.YamlFile("recyclarr");
        if (configPath is not null)
        {
            configs.Add(configPath);
        }

        return configs;
    }

    public IReadOnlyCollection<IFileInfo> GetConfigFiles()
    {
        var configs = FindDefaultConfigFiles();
        if (configs.Count == 0)
        {
            throw new NoConfigurationFilesException();
        }

        return configs;
    }
}
