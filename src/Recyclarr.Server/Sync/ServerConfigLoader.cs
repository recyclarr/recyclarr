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
    IReadOnlyList<string> DeprecationWarnings,
    IReadOnlyList<IFilterResult> FilterResults
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
    /// Loads every configured instance, logging each configuration problem and deprecation.
    /// Returns null when any configuration error exists, so an invalid configuration never
    /// produces a snapshot.
    /// </summary>
    public ServerConfiguration? TryLoadServerConfiguration()
    {
        var result = LoadConfigs();
        var diagnostics = ConfigLoadDiagnosticsBuilder.Build(result);
        ConfigLoadDiagnosticsLogger.Log(log, diagnostics);

        return diagnostics.HasServerConfigurationErrors
            ? null
            : new ServerConfiguration(result.Configs);
    }

    private ServerConfigLoadResult LoadConfigs()
    {
        var allConfigs = new List<LoadedConfigYaml>();
        var failures = new List<ConfigParsingException>();
        foreach (var file in FindConfigFiles(failures))
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
            // An include that does not resolve, for example a template a provider update removed.
            catch (YamlIncludeException e)
            {
                failures.Add(new ConfigParsingException(e.Message, line: 0, e) { FilePath = file });
            }
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
            diagnosticCollector.Deprecations,
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
