using System.IO.Abstractions;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Common;
using JetBrains.Annotations;
using Recyclarr.Command.Initialization;
using Serilog;
using TrashLib;

namespace Recyclarr.Command;

[Command("create-config", Description = "Create a starter YAML configuration file")]
[UsedImplicitly]
public class CreateConfigCommand : ICommand
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly IDefaultAppDataSetup _appDataSetup;
    private readonly ILogger _log;

    public CreateConfigCommand(ILogger logger, IFileSystem fs, IAppPaths paths, IDefaultAppDataSetup appDataSetup)
    {
        _log = logger;
        _fs = fs;
        _paths = paths;
        _appDataSetup = appDataSetup;
    }

    [CommandOption("path", 'p', Description =
        "Path where the new YAML file should be created. Must include the filename (e.g. path/to/config.yml). " +
        "File must not already exist. If not specified, uses the default path of `recyclarr.yml` in the app data " +
        "directory")]
    public string? Path { get; set; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        _appDataSetup.SetupDefaultPath(Path, true);

        var reader = new ResourceDataReader(typeof(Program));
        var ymlData = reader.ReadData("config-template.yml");
        var path = _paths.ConfigPath;

        if (_fs.File.Exists(path))
        {
            throw new CommandException($"The file {path} already exists. Please choose another path or " +
                                       "delete/move the existing file and run this command again.");
        }

        _fs.Directory.CreateDirectory(_fs.Path.GetDirectoryName(path));
        _fs.File.WriteAllText(path, ymlData);
        _log.Information("Created configuration at: {Path}", path);
        return default;
    }
}
