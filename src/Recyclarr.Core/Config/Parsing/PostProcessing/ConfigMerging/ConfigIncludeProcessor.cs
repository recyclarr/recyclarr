using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class ConfigIncludeProcessor(
    IFileSystem fs,
    IAppPaths paths,
    ConfigInputRecorder inputs,
    ILogger log
) : IIncludeProcessor
{
    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (ConfigYamlInclude)includeDirective;

        if (include.Config is null)
        {
            throw new YamlIncludeException("`config` property is required.");
        }

        var configFile = ConvertToAbsolute(include.Config);
        if (configFile?.Exists != true)
        {
            throw new YamlIncludeException(
                $"Include path could not be resolved: {include.Config}. "
                    + "Relative paths must exist in the `includes` directory."
            );
        }

        return configFile;
    }

    private IFileInfo? ConvertToAbsolute(string path)
    {
        if (fs.Path.IsPathRooted(path))
        {
            log.Debug("Path processed as absolute: {Path}", path);
            var absolute = fs.FileInfo.New(path);
            inputs.Record(absolute.FullName);
            return absolute;
        }

        var fsPath = paths.YamlIncludeDirectory.File(path);
        inputs.Record(fsPath.FullName);
        if (fsPath.Exists)
        {
            log.Debug("Path rooted to the includes directory: {Path}", path);
            return fsPath;
        }

        return null;
    }
}
