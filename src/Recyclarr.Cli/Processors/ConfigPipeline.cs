using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors;

internal class ConfigPipeline(
    IReadOnlyList<LoadedConfigYaml> allConfigs,
    IReadOnlyList<ConfigParsingException> failures,
    IConfigDiagnosticCollector diagnosticCollector,
    ConfigFilterProcessor filterProcessor,
    InstanceScopeFactory instanceScopeFactory,
    IAnsiConsole console,
    ILogger log
)
{
    private SupportedServices? _service;
    private IReadOnlyCollection<string> _instances = [];

    public ConfigPipeline FilterByService(SupportedServices? service)
    {
        if (service is not null)
        {
            _service = service;
        }

        return this;
    }

    public ConfigPipeline FilterByInstance(IReadOnlyCollection<string>? instances)
    {
        if (instances is { Count: > 0 })
        {
            _instances = instances;
        }

        return this;
    }

    public IReadOnlyList<IServiceConfiguration> GetConfigs()
    {
        var criteria = new ConfigFilterCriteria { Service = _service, Instances = _instances };

        // Extract all instance names before filtering for better error messages
        var allInstanceNames = allConfigs
            .Select(x => x.InstanceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedConfigs = allConfigs.Where(criteria.InstanceMatchesCriteria).ToList();
        var filteredConfigs = filterProcessor.FilterAndRender(
            criteria,
            matchedConfigs,
            allInstanceNames
        );

        // Convert from YAML data objects to runtime service configurations
        var configs = filteredConfigs
            .Select(x =>
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

        var result = new ConfigRegistryResult(configs, failures, diagnosticCollector.Deprecations);
        ConfigFailureRenderer.Render(console, log, result);
        return configs;
    }

    public async Task ProcessEach<TProcessor>(
        Func<TProcessor, CancellationToken, Task> action,
        CancellationToken ct
    )
        where TProcessor : notnull
    {
        foreach (var config in GetConfigs())
        {
            using var scope = instanceScopeFactory.Start<TProcessor>(config);
            await action(scope.Entry, ct);
        }
    }
}
