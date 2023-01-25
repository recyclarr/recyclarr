using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Services.Processors;

public class ConfigCreationProcessor : IConfigCreationProcessor
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly IFileSystem _fs;
    private readonly IResourceDataReader _resources;

    public ConfigCreationProcessor(
        ILogger log,
        IAppPaths paths,
        IFileSystem fs,
        IResourceDataReader resources)
    {
        _log = log;
        _paths = paths;
        _fs = fs;
        _resources = resources;
    }

    public async Task Process(string? configFilePath)
    {
        var configFile = configFilePath is null ? _paths.ConfigPath : _fs.FileInfo.New(configFilePath);
        if (configFile.Exists)
        {
            throw new FileExistsException(configFile.FullName);
        }

        configFile.Directory?.Create();
        await using var stream = configFile.CreateText();

        var ymlData = _resources.ReadData("config-template.yml");
        await stream.WriteAsync(ymlData);

        _log.Information("Created configuration at: {Path}", configFile.FullName);
    }
}
