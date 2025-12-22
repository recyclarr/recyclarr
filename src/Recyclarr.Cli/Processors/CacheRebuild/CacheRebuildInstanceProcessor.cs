using System.Globalization;
using Recyclarr.Cache;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.CacheRebuild;

internal class CacheRebuildInstanceProcessor(
    ILogger log,
    IAnsiConsole console,
    IServiceConfiguration config,
    ICustomFormatApiService customFormatApi,
    ICachePersister<CustomFormatCacheObject> cachePersister,
    ICacheStoragePath cacheStoragePath,
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery
)
{
    // State configuration: (SortPriority, FormattedDisplay)
    // Priority: lower = more interesting, shown first in verbose output
    private static readonly Dictionary<CfCacheState, (int Priority, string Format)> StateConfig =
        new()
        {
            // Error (prevents cache from being saved)
            [CfCacheState.Ambiguous] = (0, "[red]Ambiguous[/]"),

            // Cache modifications (changes that will be saved)
            [CfCacheState.Corrected] = (1, "[yellow]Corrected[/]"),
            [CfCacheState.Removed] = (2, "[maroon]Removed[/]"),
            [CfCacheState.Adopted] = (3, "[green]Adopted[/]"),

            // Informational (no cache modification)
            [CfCacheState.Skipped] = (4, "[yellow]Skipped[/]"),
            [CfCacheState.NotInService] = (5, "[blue]Not in service[/]"),
            [CfCacheState.Preserved] = (6, "[dim]Preserved[/]"),
            [CfCacheState.Unchanged] = (7, "[dim]Unchanged[/]"),
        };

    public async Task<bool> ProcessAsync(ICacheRebuildSettings settings, CancellationToken ct)
    {
        console.WriteLine();
        console.Write(
            new Rule($"[bold]{config.ServiceType}: {config.InstanceName}[/]").LeftJustified()
        );
        console.WriteLine();

        var existingCache = cachePersister.Load();
        var existingMappings = existingCache.Mappings.ToDictionary(m => m.TrashId);
        log.Debug("Loaded existing cache with {Count} entries", existingMappings.Count);

        IList<CustomFormatResource> serviceCfs = [];
        await console
            .Status()
            .StartAsync(
                "Fetching custom formats from service...",
                async _ =>
                {
                    serviceCfs = await customFormatApi.GetCustomFormats(ct);
                }
            );
        var serviceIdSet = serviceCfs.Select(cf => cf.Id).ToHashSet();
        log.Debug("Fetched {Count} custom formats from service", serviceCfs.Count);

        // Get consolidated trash_ids from all configs, then resolve to resources
        var configuredTrashIds = cfProvider
            .GetAll()
            .SelectMany(cfg => cfg.TrashIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGuideCfs = GetGuideCfsForService();
        var configuredGuideCfs = allGuideCfs
            .Where(cf => configuredTrashIds.Contains(cf.TrashId))
            .ToList();

        console.MarkupLine($"[dim]Configured: {configuredGuideCfs.Count} custom formats[/]");
        console.WriteLine();

        if (settings.Verbose)
        {
            var cachePath = cacheStoragePath.CalculatePath<CustomFormatCacheObject>();
            console.MarkupLine($"[dim]Cache file: {Markup.Escape(cachePath.FullName)}[/]");
            console.WriteLine();
        }

        var (matches, ambiguous) = MatchCustomFormats(configuredGuideCfs, serviceCfs);
        var result = BuildNewCache(
            settings,
            existingMappings,
            matches,
            ambiguous,
            configuredGuideCfs,
            serviceIdSet
        );

        ReportResults(result.Stats, ambiguous);

        if (settings.Verbose)
        {
            ReportVerboseDetails(result.Details);
        }

        if (ambiguous.Count > 0)
        {
            ReportAmbiguousErrors(ambiguous);
            return false;
        }

        if (settings.Preview)
        {
            console.MarkupLine("[yellow]Preview mode - no changes saved.[/]");
            return true;
        }

        if (result.Stats.HasChanges)
        {
            SaveCache(result.Mappings);
            console.MarkupLine($"[green]Cache saved with {result.Stats.TotalEntries} entries.[/]");
        }
        else
        {
            console.MarkupLine($"[dim]Cache unchanged ({result.Stats.TotalEntries} entries).[/]");
        }

        return true;
    }

    private static (
        List<TrashIdMapping> Matches,
        List<AmbiguousMatch> Ambiguous
    ) MatchCustomFormats(
        IReadOnlyList<CustomFormatResource> configuredGuideCfs,
        IList<CustomFormatResource> serviceCfs
    )
    {
        var matches = new List<TrashIdMapping>();
        var ambiguous = new List<AmbiguousMatch>();
        var serviceCfsByName = serviceCfs.ToLookup(cf => cf.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var guideCf in configuredGuideCfs)
        {
            var nameMatches = serviceCfsByName[guideCf.Name].Select(s => (s.Name, s.Id)).ToList();

            switch (nameMatches.Count)
            {
                case 1:
                    matches.Add(
                        new TrashIdMapping(guideCf.TrashId, guideCf.Name, nameMatches[0].Id)
                    );
                    break;
                case > 1:
                    ambiguous.Add(new AmbiguousMatch(guideCf.Name, nameMatches));
                    break;
                // case 0: "new" - no match in service, will be created on sync
            }
        }

        return (matches, ambiguous);
    }

    private static CacheRebuildResult BuildNewCache(
        ICacheRebuildSettings settings,
        Dictionary<string, TrashIdMapping> existingMappings,
        List<TrashIdMapping> matches,
        List<AmbiguousMatch> ambiguous,
        List<CustomFormatResource> configuredGuideCfs,
        HashSet<int> serviceIdSet
    )
    {
        var stats = new StatsAccumulator();
        var matchedTrashIds = matches
            .Select(m => m.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ambiguousNames = ambiguous
            .Select(a => a.GuideName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var configuredTrashIds = configuredGuideCfs
            .Select(cf => cf.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchResults = ProcessMatches(settings, matches, existingMappings, stats);
        var unmatchedDetails = BuildUnmatchedDetails(
            configuredGuideCfs,
            matchedTrashIds,
            ambiguousNames,
            stats
        );
        var (preserved, removed) = ProcessNonConfiguredEntries(
            existingMappings,
            configuredTrashIds,
            serviceIdSet,
            stats
        );

        // Only include mappings for states that should be cached (exclude Skipped)
        var mappings = matchResults
            .Where(r => r.Detail.State != CfCacheState.Skipped)
            .Select(r => r.Mapping)
            .Concat(preserved.Select(r => r.Mapping))
            .ToList();

        var details = matchResults
            .Select(r => r.Detail)
            .Concat(unmatchedDetails)
            .Concat(preserved.Select(r => r.Detail))
            .Concat(removed)
            .ToList();

        return new CacheRebuildResult(mappings, stats.ToStats(), details);
    }

    private static List<(TrashIdMapping Mapping, CfCacheDetail Detail)> ProcessMatches(
        ICacheRebuildSettings settings,
        List<TrashIdMapping> matches,
        Dictionary<string, TrashIdMapping> existingMappings,
        StatsAccumulator stats
    )
    {
        return matches
            .Select(match =>
            {
                existingMappings.TryGetValue(match.TrashId, out var existing);
                var state = ClassifyMatchState(settings, existing, match, stats);
                var detail = new CfCacheDetail(
                    match.Name,
                    match.TrashId,
                    match.ServiceId,
                    existing?.ServiceId,
                    state
                );
                return (match, detail);
            })
            .ToList();
    }

    private static CfCacheState ClassifyMatchState(
        ICacheRebuildSettings settings,
        TrashIdMapping? existing,
        TrashIdMapping match,
        StatsAccumulator stats
    )
    {
        if (existing is null)
        {
            // No cache entry exists - only adopt if --adopt flag is set
            if (settings.Adopt)
            {
                stats.RecordAdopted();
                return CfCacheState.Adopted;
            }

            stats.RecordSkipped();
            return CfCacheState.Skipped;
        }

        if (existing.ServiceId == match.ServiceId)
        {
            stats.RecordUnchanged();
            return CfCacheState.Unchanged;
        }

        stats.RecordCorrected();
        return CfCacheState.Corrected;
    }

    private static IEnumerable<CfCacheDetail> BuildUnmatchedDetails(
        List<CustomFormatResource> configuredGuideCfs,
        HashSet<string> matchedTrashIds,
        HashSet<string> ambiguousNames,
        StatsAccumulator stats
    )
    {
        return configuredGuideCfs
            .Where(cf => !matchedTrashIds.Contains(cf.TrashId))
            .Select(cf =>
            {
                var isAmbiguous = ambiguousNames.Contains(cf.Name);
                if (!isAmbiguous)
                {
                    stats.RecordNotInService();
                }

                return new CfCacheDetail(
                    cf.Name,
                    cf.TrashId,
                    ServiceId: null,
                    CachedServiceId: null,
                    isAmbiguous ? CfCacheState.Ambiguous : CfCacheState.NotInService
                );
            });
    }

    private static (
        List<(TrashIdMapping Mapping, CfCacheDetail Detail)> Preserved,
        List<CfCacheDetail> Removed
    ) ProcessNonConfiguredEntries(
        Dictionary<string, TrashIdMapping> existingMappings,
        HashSet<string> configuredTrashIds,
        HashSet<int> serviceIdSet,
        StatsAccumulator stats
    )
    {
        var preserved = new List<(TrashIdMapping Mapping, CfCacheDetail Detail)>();
        var removed = new List<CfCacheDetail>();

        foreach (
            var entry in existingMappings.Values.Where(e => !configuredTrashIds.Contains(e.TrashId))
        )
        {
            if (serviceIdSet.Contains(entry.ServiceId))
            {
                stats.RecordPreserved();
                var detail = new CfCacheDetail(
                    entry.Name,
                    entry.TrashId,
                    entry.ServiceId,
                    entry.ServiceId, // Cached ID same as current - no change
                    CfCacheState.Preserved
                );
                preserved.Add((entry, detail));
            }
            else
            {
                stats.RecordRemoved();
                var detail = new CfCacheDetail(
                    entry.Name,
                    entry.TrashId,
                    ServiceId: null, // Service CF no longer exists
                    entry.ServiceId, // Was cached with this ID
                    CfCacheState.Removed
                );
                removed.Add(detail);
            }
        }

        return (preserved, removed);
    }

    private void ReportResults(CacheRebuildStats stats, List<AmbiguousMatch> ambiguous)
    {
        // Changes section
        console.MarkupLine("[bold underline]Changes[/]");
        if (stats.HasChanges)
        {
            var changesGrid = new Grid().AddColumn().AddColumn();
            if (stats.Adopted > 0)
            {
                changesGrid.AddRow("[green]Adopted:[/]", $"{stats.Adopted}");
            }

            if (stats.Corrected > 0)
            {
                changesGrid.AddRow("[yellow]Corrected:[/]", $"{stats.Corrected}");
            }

            if (stats.Removed > 0)
            {
                changesGrid.AddRow("[maroon]Removed:[/]", $"{stats.Removed}");
            }

            console.Write(changesGrid);
        }
        else
        {
            console.MarkupLine("[dim]None - cache already correct[/]");
        }

        console.WriteLine();

        // Summary section
        console.MarkupLine("[bold underline]Summary[/]");
        var summaryGrid = new Grid().AddColumn().AddColumn();

        if (stats.Skipped > 0)
        {
            summaryGrid.AddRow("[yellow]Skipped:[/]", $"{stats.Skipped} (use --adopt to add)");
        }

        if (stats.Unchanged > 0)
        {
            summaryGrid.AddRow("[dim]Unchanged:[/]", $"{stats.Unchanged}");
        }

        if (stats.NotInService > 0)
        {
            summaryGrid.AddRow(
                "[dim]Not in service:[/]",
                $"{stats.NotInService} (will be created on sync)"
            );
        }

        if (stats.Preserved > 0)
        {
            summaryGrid.AddRow("[dim]Preserved:[/]", $"{stats.Preserved} (kept for sync deletion)");
        }

        if (ambiguous.Count > 0)
        {
            summaryGrid.AddRow("[red]Ambiguous:[/]", $"{ambiguous.Count}");
        }

        console.Write(summaryGrid);
        console.WriteLine();
    }

    private void ReportVerboseDetails(List<CfCacheDetail> details)
    {
        var table = new Table()
            .AddColumn("Name")
            .AddColumn("Trash ID")
            .AddColumn("Service ID")
            .AddColumn("State")
            .BorderColor(Color.Grey);

        foreach (
            var detail in details
                .OrderBy(d => GetStatePriority(d.State))
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
        )
        {
            var serviceIdText = FormatServiceId(detail);
            var stateText = FormatState(detail.State);
            table.AddRow(Markup.Escape(detail.Name), detail.TrashId, serviceIdText, stateText);
        }

        console.Write(table);
        console.WriteLine();
    }

    private static string FormatServiceId(CfCacheDetail detail)
    {
        var hasServiceId = detail.ServiceId.HasValue;
        var hasCachedId = detail.CachedServiceId.HasValue;

        return (hasServiceId, hasCachedId) switch
        {
            (false, false) => "[dim]-[/]",
            (false, true) => $"[dim]{detail.CachedServiceId}[/]", // Removed: was cached, now gone
            (true, false) => detail.ServiceId!.Value.ToString(CultureInfo.InvariantCulture),
            (true, true) when detail.ServiceId == detail.CachedServiceId =>
                detail.ServiceId!.Value.ToString(CultureInfo.InvariantCulture),
            (true, true) => // Corrected: cached ID differs from service ID
            $"[dim]{detail.CachedServiceId}[/] [yellow]→[/] {detail.ServiceId}",
        };
    }

    private static int GetStatePriority(CfCacheState state) =>
        StateConfig.TryGetValue(state, out var cfg) ? cfg.Priority : 99;

    private static string FormatState(CfCacheState state) =>
        StateConfig.TryGetValue(state, out var cfg) ? cfg.Format : state.ToString();

    private void ReportAmbiguousErrors(List<AmbiguousMatch> ambiguous)
    {
        var table = new Table()
            .AddColumn("Guide CF")
            .AddColumn("Service Matches")
            .BorderColor(Color.Red);

        foreach (var match in ambiguous)
        {
            var serviceMatches = string.Join(
                ", ",
                match.ServiceMatches.Select(m => $"\"{m.Name}\" (id {m.Id})")
            );
            table.AddRow(Markup.Escape(match.GuideName), serviceMatches);
        }

        var panel = new Panel(table)
            .Header("[red]Ambiguous Custom Format Names[/]")
            .BorderColor(Color.Red);

        console.Write(panel);
        console.WriteLine();
        console.MarkupLine(
            $"[dim]Resolution: Delete or rename duplicate CFs in {config.ServiceType}, then retry.[/]"
        );
        console.MarkupLine("[red]Cache NOT saved for this instance.[/]");
    }

    private void SaveCache(List<TrashIdMapping> matches)
    {
        log.Debug("Saving rebuilt cache with {Count} mappings", matches.Count);

        var cacheObject = new CustomFormatCacheObject { Mappings = matches };
        var cache = new TrashIdCache<CustomFormatCacheObject>(cacheObject);
        cachePersister.Save(cache);
    }

    private IReadOnlyList<CustomFormatResource> GetGuideCfsForService()
    {
        return config.ServiceType switch
        {
            SupportedServices.Radarr => cfQuery.GetRadarr(),
            SupportedServices.Sonarr => cfQuery.GetSonarr(),
            _ => throw new InvalidOperationException($"Unknown service type: {config.ServiceType}"),
        };
    }
}
