using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Server.Sync;

// Loaded instances plus the parse failures, filter diagnostics, and deprecations found while
// loading them.
internal sealed record ServerConfigLoadResult(
    IReadOnlyList<IServiceConfiguration> Configs,
    IReadOnlyList<ConfigParsingException> Failures,
    IReadOnlyList<ConfigDeprecation> DeprecationWarnings,
    IReadOnlyList<IFilterResult> FilterResults
);

/// <summary>
/// The configuration a load produced, or null when it had errors, plus every problem the load
/// found either way.
/// </summary>
internal sealed record ServerConfigLoad(
    ServerConfiguration? Configuration,
    ConfigLoadDiagnostics Diagnostics
);

// Loads the server's configuration directory and resolves it against the guide data bound to the
// current scope (ADR-019). Parse failures and filter diagnostics are collected and logged.
internal sealed class ServerConfigLoader(
    ILogger log,
    IConfigurationFinder finder,
    ConfigurationLoader loader,
    ConfigFilterProcessor filterProcessor,
    IConfigDiagnosticCollector diagnosticCollector
)
{
    /// <summary>
    /// Loads every configured instance, logging each configuration problem and deprecation. The
    /// configuration is null when any configuration error exists, so an invalid configuration
    /// never produces a snapshot.
    /// </summary>
    public ServerConfigLoad LoadServerConfiguration()
    {
        var result = LoadConfigs();
        var diagnostics = ConfigLoadDiagnosticsBuilder.Build(result);
        ConfigLoadDiagnosticsLogger.Log(log, diagnostics);

        var configuration = diagnostics.HasServerConfigurationErrors
            ? null
            : new ServerConfiguration(result.Configs);
        return new ServerConfigLoad(configuration, diagnostics);
    }

    private ServerConfigLoadResult LoadConfigs()
    {
        var allConfigs = new List<LoadedConfigYaml>();
        var failures = new List<ConfigParsingException>();
        var deprecations = new List<ConfigDeprecation>();
        foreach (var file in FindConfigFiles(failures))
        {
            // The collector is shared by every file in this load, so the deprecations a file adds
            // are the ones past the count before it loaded.
            var deprecationsBefore = diagnosticCollector.Deprecations.Count;
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
            // An include that does not resolve, for example a template a provider update removed.
            catch (YamlIncludeException e)
            {
                failures.Add(new ConfigParsingException(e.Message, line: 0, e) { FilePath = file });
            }

            deprecations.AddRange(
                diagnosticCollector
                    .Deprecations.Skip(deprecationsBefore)
                    .Select(x => new ConfigDeprecation(file.Name, x))
            );
        }

        var allInstanceNames = allConfigs
            .Select(x => x.InstanceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var filterResult = filterProcessor.Filter(
            new ConfigFilterCriteria(),
            allConfigs,
            allInstanceNames
        );

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
            deprecations,
            filterResult.FilterResults.ToList()
        );
    }

    // A server without configuration still serves guide data; it has nothing to sync.
    private IReadOnlyCollection<IFileInfo> FindConfigFiles(List<ConfigParsingException> failures)
    {
        try
        {
            return finder.GetConfigFiles();
        }
        catch (NoConfigurationFilesException)
        {
            return [];
        }
        catch (ConflictingYamlFilesException e)
        {
            failures.Add(new ConfigParsingException(e.Message, line: 0, e));
            return [];
        }
    }
}
