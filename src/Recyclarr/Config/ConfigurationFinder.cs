using System.IO.Abstractions;
using CliFx.Exceptions;
using Serilog;
using TrashLib.Startup;

namespace Recyclarr.Config;

public class ConfigurationFinder : IConfigurationFinder
{
    private readonly IAppPaths _paths;
    private readonly IFileSystem _fs;
    private readonly ILogger _log;

    public ConfigurationFinder(IAppPaths paths, IFileSystem fs, ILogger log)
    {
        _paths = paths;
        _fs = fs;
        _log = log;
    }

    private IReadOnlyCollection<string> FindDefaultConfigFiles()
    {
        var configs = new List<string>();

        if (_paths.ConfigsDirectory.Exists)
        {
            configs.AddRange(_paths.ConfigsDirectory.EnumerateFiles("*.yml").Select(x => x.FullName));
        }

        if (_paths.ConfigPath.Exists)
        {
            configs.Add(_paths.ConfigPath.FullName);
        }

        return configs;
    }

    public IReadOnlyCollection<string> GetConfigFiles(IReadOnlyCollection<string>? configs)
    {
        if (configs is null || !configs.Any())
        {
            configs = FindDefaultConfigFiles();
        }
        else
        {
            var split = configs.ToLookup(x => _fs.File.Exists(x));
            foreach (var nonExistentConfig in split[false])
            {
                _log.Warning("Configuration file does not exist {File}", nonExistentConfig);
            }

            configs = split[true].ToList();
        }

        if (configs.Count == 0)
        {
            throw new CommandException("No configuration YAML files found");
        }

        _log.Debug("Using config files: {ConfigFiles}", configs);
        return configs;
    }
}
