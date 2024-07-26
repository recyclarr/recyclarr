using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class ConfigIncludeProcessor(IFileSystem fs, IAppPaths paths, ILogger log) : IIncludeProcessor
{
    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (ConfigYamlInclude) includeDirective;

        if (include.Config is null)
        {
            throw new YamlIncludeException("`config` property is required.");
        }

        var configFile = ConvertToAbsolute(include.Config);
        if (configFile?.Exists != true)
        {
            throw new YamlIncludeException($"Include path could not be resolved: {include.Config}");
        }

        return configFile;
    }

    private IFileInfo? ConvertToAbsolute(string path)
    {
        if (fs.Path.IsPathRooted(path))
        {
            log.Debug("Path processed as absolute: {Path}", path);
            return fs.FileInfo.New(path);
        }

        var fsPath = paths.IncludesDirectory.File(path);
        if (fsPath.Exists)
        {
            log.Debug("Path rooted to the includes directory: {Path}", path);
            return fsPath;
        }

        fsPath = paths.ConfigsDirectory.File(path);
        // ReSharper disable once InvertIf
        if (fsPath.Exists)
        {
            log.Warning(
                "DEPRECATED: Include templates inside the `configs` directory are no longer supported. " +
                "These files should be relocated to the new sibling `includes` directory instead. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v8.0/#include-dir");

            return fsPath;
        }

        return null;
    }
}
