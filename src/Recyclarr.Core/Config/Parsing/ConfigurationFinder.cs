using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Platform;

namespace Recyclarr.Config.Parsing;

public class ConfigurationFinder(IAppPaths paths) : IConfigurationFinder
{
    private List<IFileInfo> FindDefaultConfigFiles()
    {
        var configs = new List<IFileInfo>();

        if (paths.ConfigsDirectory.Exists)
        {
            var extensions = new[] { "*.yml", "*.yaml" };
            var files = extensions.SelectMany(x => paths.ConfigsDirectory.EnumerateFiles(x));
            configs.AddRange(files);
        }

        var configPath = paths.AppDataDirectory.YamlFile("recyclarr");
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
