using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Parsing;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

/// <remarks>
/// This was originally intended to be used by `config create`, but YamlDotNet cannot serialize
/// comments so this
/// class was not used. I kept it around in case I want to revisit later. There might be an
/// opportunity to use this
/// with the GUI.
/// </remarks>
public class ConfigManipulator : IConfigManipulator
{
    private readonly IAnsiConsole _console;
    private readonly ConfigParser _configParser;
    private readonly ConfigSaver _configSaver;
    private readonly ConfigValidationExecutor _validator;

    public ConfigManipulator(
        IAnsiConsole console,
        ConfigParser configParser,
        ConfigSaver configSaver,
        ConfigValidationExecutor validator)
    {
        _console = console;
        _configParser = configParser;
        _configSaver = configSaver;
        _validator = validator;
    }

    private static IReadOnlyDictionary<string, TConfig> InvokeCallbackForEach<TConfig>(
        Func<string, ServiceConfigYaml, ServiceConfigYaml> editCallback,
        IReadOnlyDictionary<string, TConfig>? configs)
        where TConfig : ServiceConfigYaml
    {
        var newConfigs = new Dictionary<string, TConfig>();

        if (configs is null)
        {
            return newConfigs;
        }

        foreach (var (instanceName, config) in configs)
        {
            newConfigs[instanceName] = (TConfig) editCallback(instanceName, config);
        }

        return newConfigs;
    }

    public void LoadAndSave(
        IFileInfo source,
        IFileInfo destinationFile,
        Func<string, ServiceConfigYaml, ServiceConfigYaml> editCallback)
    {
        // Parse & save the template file to address the following:
        // - Find & report any syntax errors
        // - Run validation & report issues
        // - Consistently reformat the output file (when it is saved again)
        // - Ignore stuff for diffing purposes, such as comments.
        var config = _configParser.Load(source);
        if (config is null)
        {
            // Do not log here, since ConfigParser already has substantial logging
            throw new FileLoadException("Problem while loading config template");
        }

        config = new RootConfigYaml
        {
            Radarr = InvokeCallbackForEach(editCallback, config.Radarr),
            Sonarr = InvokeCallbackForEach(editCallback, config.Sonarr)
        };

        if (!_validator.Validate(config))
        {
            _console.WriteLine(
                "The configuration file will still be created, despite the previous validation errors. " +
                "You must open the file and correct the above issues before running a sync command.");
        }

        _configSaver.Save(config, destinationFile);
    }
}
