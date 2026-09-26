using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Server.Sync;

// Structured equivalent of Recyclarr.Cli's ConfigRegistryResult, plus the filter diagnostics
// that ConfigFilterProcessor produces. Callers (endpoints) decide how to translate this into
// HTTP responses and job diagnostics; this type carries no rendering concerns.
internal sealed record ServerConfigLoadResult(
    IReadOnlyList<IServiceConfiguration> Configs,
    IReadOnlyList<ConfigParsingException> Failures,
    IReadOnlyList<string> DeprecationWarnings,
    IReadOnlyList<IFilterResult> FilterResults
)
{
    public bool HasAvailableConfigs { get; init; }
}

// Server-side equivalent of Recyclarr.Cli's ConfigPipeline/ConfigPipelineFactory, minus CLI
// rendering concerns (IAnsiConsole, ConfigFailureRenderer). Parse failures and filter
// diagnostics are collected into ServerConfigLoadResult instead of being rendered or thrown.
internal sealed class ServerConfigLoader(
    IConfigurationFinder finder,
    ConfigurationLoader loader,
    ConfigFilterProcessor filterProcessor,
    IConfigDiagnosticCollector diagnosticCollector
)
{
    public ServerConfigLoadResult LoadConfigs(ServerSyncSettings settings)
    {
        var allConfigs = new List<LoadedConfigYaml>();
        var failures = new List<ConfigParsingException>();
        foreach (var file in finder.GetConfigFiles())
        {
            try
            {
                allConfigs.AddRange(loader.Load(file));
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

        var criteria = new ConfigFilterCriteria
        {
            Service = settings.Service,
            Instances = settings.Instances,
        };
        var allInstanceNames = allConfigs
            .Select(x => x.InstanceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedConfigs = allConfigs.Where(criteria.InstanceMatchesCriteria).ToList();
        var filterResult = filterProcessor.Filter(criteria, matchedConfigs, allInstanceNames);

        var configs = filterResult
            .Configs.Select(x =>
                x.Yaml switch
                {
                    RadarrConfigYaml radarr => radarr.ToRadarrConfiguration(
                        x.InstanceName,
                        x.YamlPath
                    ),
                    SonarrConfigYaml sonarr => sonarr.ToSonarrConfiguration(
                        x.InstanceName,
                        x.YamlPath
                    ),
                    _ => throw new InvalidOperationException("Unknown config type"),
                }
            )
            .ToList();

        return new ServerConfigLoadResult(
            configs,
            failures,
            diagnosticCollector.Deprecations,
            filterResult.FilterResults.ToList()
        )
        {
            HasAvailableConfigs = allConfigs.Count > 0,
        };
    }
}
