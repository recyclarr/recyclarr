using System.IO.Abstractions;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Config.Parsing;

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
            configs.AddRange(_paths.ConfigsDirectory.EnumerateFiles("*.yml"));
        }

        if (_paths.ConfigPath.Exists)
        {
            configs.Add(_paths.ConfigPath);
        }

        return configs;
    }

    public IReadOnlyCollection<IFileInfo> GetConfigFiles(IReadOnlyCollection<IFileInfo>? configs)
    {
        if (configs is not null && configs.Any())
        {
            return configs;
        }

        configs = FindDefaultConfigFiles();
        if (configs.Count == 0)
        {
            throw new NoConfigurationFilesException();
        }

        return configs;
    }
}
