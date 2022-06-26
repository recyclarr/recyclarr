using System.IO.Abstractions;
using CliFx.Attributes;
using CliFx.Exceptions;
using Common;
using JetBrains.Annotations;
using Serilog;
using TrashLib;

namespace Recyclarr.Command;

[Command("create-config", Description = "Create a starter YAML configuration file")]
[UsedImplicitly]
public class CreateConfigCommand : BaseCommand
{
    [CommandOption("path", 'p', Description =
        "Path where the new YAML file should be created. Must include the filename (e.g. path/to/config.yml). " +
        "File must not already exist. If not specified, uses the default path of `recyclarr.yml` in the app data " +
        "directory")]
    public override string? AppDataDirectory { get; set; }

    public override async Task Process(IServiceLocatorProxy container)
    {
        var fs = container.Resolve<IFileSystem>();
        var paths = container.Resolve<IAppPaths>();
        var log = container.Resolve<ILogger>();

        var reader = new ResourceDataReader(typeof(Program));
        var ymlData = reader.ReadData("config-template.yml");
        var configFile = AppDataDirectory is not null
            ? fs.FileInfo.FromFileName(AppDataDirectory)
            : paths.ConfigPath;

        if (configFile.Exists)
        {
            throw new CommandException($"The file {configFile} already exists. Please choose another path or " +
                                       "delete/move the existing file and run this command again.");
        }

        fs.Directory.CreateDirectory(configFile.DirectoryName);
        await using var stream = configFile.CreateText();
        await stream.WriteAsync(ymlData);
        log.Information("Created configuration at: {Path}", configFile);
    }
}
