using System.IO.Abstractions;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Common;
using JetBrains.Annotations;
using Serilog;
using TrashLib;

namespace Recyclarr.Command;

[Command("create-config", Description = "Create a starter YAML configuration file")]
[UsedImplicitly]
public class CreateConfigCommand : ICommand
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly ILogger _log;
    private string? _path;

    public CreateConfigCommand(ILogger logger, IFileSystem fs, IAppPaths paths)
    {
        _log = logger;
        _fs = fs;
        _paths = paths;
    }

    [CommandOption("path", 'p', Description =
        "Path where the new YAML file should be created. Must include the filename (e.g. path/to/config.yml). " +
        "File must not already exist. If not specified, uses the default path of `recyclarr.yml` right next to the " +
        "executable.")]
    public string Path
    {
        get => _path ?? _paths.ConfigPath;
        set => _path = value;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var reader = new ResourceDataReader(typeof(Program));
        var ymlData = reader.ReadData("config-template.yml");

        if (_fs.File.Exists(Path))
        {
            throw new CommandException($"The file {Path} already exists. Please choose another path or " +
                                       "delete/move the existing file and run this command again.");
        }

        _fs.Directory.CreateDirectory(_fs.Path.GetDirectoryName(Path));
        _fs.File.WriteAllText(Path, ymlData);
        _log.Information("Created configuration at: {Path}", Path);
        return default;
    }
}
