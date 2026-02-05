using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

internal class LocalConfigCreator(
    ILogger log,
    IAnsiConsole console,
    IAppPaths paths,
    IFileSystem fs,
    IResourceDataReader resources
) : IConfigCreator
{
    public bool CanHandle(ICreateConfigSettings settings)
    {
        return true;
    }

    public void Create(ICreateConfigSettings settings)
    {
        var configFile = settings.Path is null
            ? paths.ConfigDirectory.File("recyclarr.yml")
            : fs.FileInfo.New(settings.Path);

        if (configFile.Exists)
        {
            throw new FileExistsException(configFile.FullName);
        }

        configFile.CreateParentDirectory();
        using var stream = configFile.CreateText();

        var ymlData = resources.ReadData("config-template.yml");
        stream.Write(ymlData);

        log.Information("Created configuration at: {Path}", configFile.FullName);
        console.MarkupLineInterpolated($"[green]Created:[/] {configFile.FullName}");
    }
}
