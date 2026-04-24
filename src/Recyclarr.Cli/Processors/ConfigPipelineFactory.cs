using System.IO.Abstractions;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Spectre.Console;

namespace Recyclarr.Cli.Processors;

internal class ConfigPipelineFactory(
    ConfigurationLoader loader,
    IConfigurationFinder finder,
    IFileSystem fs,
    ConfigFilterProcessor filterProcessor,
    IConfigDiagnosticCollector diagnosticCollector,
    InstanceScopeFactory instanceScopeFactory,
    IAnsiConsole console,
    ILogger log
)
{
    public ConfigPipeline FromDefaultPaths()
    {
        return Load(finder.GetConfigFiles());
    }

    public ConfigPipeline FromPaths(IReadOnlyCollection<string> paths)
    {
        var files = paths.Select(x => fs.FileInfo.New(x)).ToLookup(x => x.Exists);

        if (files[false].Any())
        {
            throw new InvalidConfigurationFilesException(files[false].ToList());
        }

        return Load(files[true]);
    }

    private ConfigPipeline Load(IEnumerable<IFileInfo> files)
    {
        var allLoadedConfigs = new List<LoadedConfigYaml>();
        var failures = new List<ConfigParsingException>();

        foreach (var file in files)
        {
            try
            {
                allLoadedConfigs.AddRange(loader.Load(file));
            }
            catch (ConfigParsingException e)
            {
                e.FilePath = file;
                failures.Add(e);
            }
            catch (YamlIncludeException e) when (e.InnerException is ConfigParsingException inner)
            {
                inner.FilePath = file;
                failures.Add(inner);
            }
        }

        return new ConfigPipeline(
            allLoadedConfigs,
            failures,
            diagnosticCollector,
            filterProcessor,
            instanceScopeFactory,
            console,
            log
        );
    }
}
