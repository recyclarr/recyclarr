using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Processors.Config;

internal class LocalConfigCreator(
    ILogger log,
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
            ? paths.AppDataDirectory.File("recyclarr.yml")
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
    }
}
