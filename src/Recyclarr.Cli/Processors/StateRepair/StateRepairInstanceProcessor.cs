using System.Globalization;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;
using Recyclarr.SyncState;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.StateRepair;

internal class StateRepairInstanceProcessor(
    ILogger log,
    IAnsiConsole console,
    IServiceConfiguration config,
    IEnumerable<IResourceAdapter> adapters
)
{
    // State configuration: (SortPriority, Color, Label, Hint)
    private static readonly Dictionary<StateRepairState, StateDisplayConfig> StateConfig = new()
    {
        // Error (prevents state from being saved)
        [StateRepairState.Ambiguous] = new StateDisplayConfig(0, Color.Red, "Ambiguous", null),

        // State modifications (changes that will be saved)
        [StateRepairState.Corrected] = new StateDisplayConfig(1, Color.Yellow, "Corrected", null),
        [StateRepairState.Removed] = new StateDisplayConfig(2, Color.Maroon, "Removed", null),
        [StateRepairState.Adopted] = new StateDisplayConfig(3, Color.Green, "Adopted", null),

        // Informational (no state modification)
        [StateRepairState.Skipped] = new StateDisplayConfig(
            4,
            Color.Yellow,
            "Skipped",
            "use --adopt to add"
        ),
        [StateRepairState.NotInService] = new StateDisplayConfig(
            5,
            Color.Blue,
            "Not in service",
            "will be created on sync"
        ),
        [StateRepairState.Preserved] = new StateDisplayConfig(
            6,
            Color.Grey,
            "Preserved",
            "kept for sync deletion"
        ),
        [StateRepairState.Unchanged] = new StateDisplayConfig(7, Color.Grey, "Unchanged", null),
    };

    private sealed record StateDisplayConfig(int Priority, Color Color, string Label, string? Hint);

    public async Task<bool> ProcessAsync(IStateRepairSettings settings, CancellationToken ct)
    {
        log.Information(
            "Rebuilding state for {ServiceType} instance {InstanceName}",
            config.ServiceType,
            config.InstanceName
        );

        console.WriteLine();
        console.Write(
            new Rule($"[bold]{config.ServiceType}: {config.InstanceName}[/]").LeftJustified()
        );
        console.WriteLine();

        var allSucceeded = true;
        var filteredAdapters = adapters.Where(a =>
            settings.Resource is null || a.ResourceType == settings.Resource
        );

        foreach (var adapter in filteredAdapters)
        {
            var success = await ProcessAdapterAsync(adapter, settings, ct);
            if (!success)
            {
                allSucceeded = false;
            }
        }

        if (settings.Preview)
        {
            console.MarkupLine("[yellow]Preview mode - no changes saved.[/]");
        }

        return allSucceeded;
    }

    private async Task<bool> ProcessAdapterAsync(
        IResourceAdapter adapter,
        IStateRepairSettings settings,
        CancellationToken ct
    )
    {
        var existingMappings = adapter.LoadExistingMappings();
        log.Debug(
            "Loaded existing {ResourceType} state with {Count} entries",
            adapter.ResourceTypeName,
            existingMappings.Count
        );

        IReadOnlyList<IServiceResource> serviceResources = [];
        await console
            .Status()
            .StartAsync(
                $"Fetching {adapter.ResourceTypeName} from service...",
                async _ =>
                {
                    serviceResources = await adapter.FetchServiceResourcesAsync(ct);
                }
            );
        var serviceIdSet = serviceResources.Select(r => r.Id).ToHashSet();
        log.Debug(
            "Fetched {Count} {ResourceType} from service",
            serviceResources.Count,
            adapter.ResourceTypeName
        );

        var configuredGuideResources = adapter.GetConfiguredGuideResources();

        // Skip empty resource types (no configured resources and no existing state)
        if (configuredGuideResources.Count == 0 && existingMappings.Count == 0)
        {
            if (settings.Verbose)
            {
                console.MarkupLine(
                    $"[dim]No {adapter.ResourceTypeName} configured (state: {Markup.Escape(adapter.GetStateFilePath())})[/]"
                );
                console.WriteLine();
            }

            return true;
        }

        var matchResult = TrashIdMappingMatcher.Match(configuredGuideResources, serviceResources);
        var result = BuildNewCache(
            settings,
            existingMappings,
            matchResult.Matches.ToList(),
            matchResult.Ambiguous.ToList(),
            configuredGuideResources,
            serviceIdSet
        );

        RenderResultTree(adapter, settings, result, configuredGuideResources.Count);

        if (matchResult.Ambiguous.Count > 0)
        {
            log.Warning(
                "State rebuild failed: {Count} ambiguous {ResourceType} names detected",
                matchResult.Ambiguous.Count,
                adapter.ResourceTypeName
            );
            ReportAmbiguousErrors(adapter, matchResult.Ambiguous);
            return false;
        }

        if (settings.Preview)
        {
            return true;
        }

        if (result.Stats.HasChanges)
        {
            adapter.SaveMappings(result.Mappings);
            log.Information(
                "{ResourceType} state rebuilt: {Adopted} adopted, {Corrected} corrected, {Removed} removed",
                adapter.ResourceTypeName,
                result.Stats.Adopted,
                result.Stats.Corrected,
                result.Stats.Removed
            );
            console.MarkupLine($"[green]State saved with {result.Stats.TotalEntries} entries.[/]");
            console.WriteLine();
        }
        else
        {
            log.Information(
                "{ResourceType} state unchanged ({Count} entries)",
                adapter.ResourceTypeName,
                result.Stats.TotalEntries
            );
            console.MarkupLine($"[dim]State unchanged ({result.Stats.TotalEntries} entries).[/]");
            console.WriteLine();
        }

        return true;
    }

    private static StateRepairResult BuildNewCache(
        IStateRepairSettings settings,
        Dictionary<string, TrashIdMapping> existingMappings,
        List<TrashIdMapping> matches,
        List<AmbiguousMatch> ambiguous,
        IReadOnlyList<IGuideResource> configuredGuideResources,
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
        var configuredTrashIds = configuredGuideResources
            .Select(r => r.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build lookup of orphan entries by service ID (entries not in current config)
        var orphansByServiceId = existingMappings
            .Values.Where(e => !configuredTrashIds.Contains(e.TrashId))
            .GroupBy(e => e.ServiceId)
            .ToDictionary(g => g.Key, g => g.First());

        var (matchResults, correctedServiceIds) = ProcessMatches(
            settings,
            matches,
            existingMappings,
            orphansByServiceId,
            stats
        );

        var unmatchedDetails = BuildUnmatchedDetails(
            configuredGuideResources,
            matchedTrashIds,
            ambiguousNames,
            stats
        );
        var (preserved, removed) = ProcessNonConfiguredEntries(
            existingMappings,
            configuredTrashIds,
            serviceIdSet,
            correctedServiceIds,
            stats
        );

        // Only include mappings for states that should be cached (exclude Skipped)
        var mappings = matchResults
            .Where(r => r.Detail.State != StateRepairState.Skipped)
            .Select(r => r.Mapping)
            .Concat(preserved.Select(r => r.Mapping))
            .ToList();

        var details = matchResults
            .Select(r => r.Detail)
            .Concat(unmatchedDetails)
            .Concat(preserved.Select(r => r.Detail))
            .Concat(removed)
            .ToList();

        return new StateRepairResult(mappings, stats.ToStats(), details);
    }

    private static (
        List<(TrashIdMapping Mapping, StateRepairDetail Detail)> Results,
        HashSet<int> CorrectedServiceIds
    ) ProcessMatches(
        IStateRepairSettings settings,
        List<TrashIdMapping> matches,
        Dictionary<string, TrashIdMapping> existingMappings,
        Dictionary<int, TrashIdMapping> orphansByServiceId,
        StatsAccumulator stats
    )
    {
        var correctedServiceIds = new HashSet<int>();
        var results = matches
            .Select(match =>
            {
                existingMappings.TryGetValue(match.TrashId, out var existing);
                orphansByServiceId.TryGetValue(match.ServiceId, out var orphan);

                var (state, cachedTrashId, cachedServiceId) = ClassifyMatchState(
                    settings,
                    existing,
                    orphan,
                    match,
                    stats
                );

                // Track service IDs that were corrected (trash_id fix from orphan)
                if (state == StateRepairState.Corrected && orphan is not null)
                {
                    correctedServiceIds.Add(match.ServiceId);
                }

                var detail = new StateRepairDetail(
                    match.Name,
                    match.TrashId,
                    cachedTrashId,
                    match.ServiceId,
                    cachedServiceId,
                    state
                );
                return (match, detail);
            })
            .ToList();

        return (results, correctedServiceIds);
    }

    private static (
        StateRepairState State,
        string? CachedTrashId,
        int? CachedServiceId
    ) ClassifyMatchState(
        IStateRepairSettings settings,
        TrashIdMapping? existing,
        TrashIdMapping? orphan,
        TrashIdMapping match,
        StatsAccumulator stats
    )
    {
        if (existing is not null)
        {
            // State entry exists for this trash_id
            if (existing.ServiceId == match.ServiceId)
            {
                stats.RecordUnchanged();
                return (StateRepairState.Unchanged, null, null);
            }

            // Service ID changed - correct it
            stats.RecordCorrected();
            return (StateRepairState.Corrected, null, existing.ServiceId);
        }

        // No state entry for this trash_id - check if an orphan owns this service ID
        if (orphan is not null)
        {
            // Orphan owns this service ID - correct the trash_id (regardless of --adopt)
            stats.RecordCorrected();
            return (StateRepairState.Corrected, orphan.TrashId, null);
        }

        // No state entry at all - adopt or skip
        if (settings.Adopt)
        {
            stats.RecordAdopted();
            return (StateRepairState.Adopted, null, null);
        }

        stats.RecordSkipped();
        return (StateRepairState.Skipped, null, null);
    }

    private static IEnumerable<StateRepairDetail> BuildUnmatchedDetails(
        IReadOnlyList<IGuideResource> configuredGuideResources,
        HashSet<string> matchedTrashIds,
        HashSet<string> ambiguousNames,
        StatsAccumulator stats
    )
    {
        return configuredGuideResources
            .Where(r => !matchedTrashIds.Contains(r.TrashId))
            .Select(r =>
            {
                var isAmbiguous = ambiguousNames.Contains(r.Name);
                if (!isAmbiguous)
                {
                    stats.RecordNotInService();
                }

                return new StateRepairDetail(
                    r.Name,
                    r.TrashId,
                    CachedTrashId: null,
                    ServiceId: null,
                    CachedServiceId: null,
                    isAmbiguous ? StateRepairState.Ambiguous : StateRepairState.NotInService
                );
            });
    }

    private static (
        List<(TrashIdMapping Mapping, StateRepairDetail Detail)> Preserved,
        List<StateRepairDetail> Removed
    ) ProcessNonConfiguredEntries(
        Dictionary<string, TrashIdMapping> existingMappings,
        HashSet<string> configuredTrashIds,
        HashSet<int> serviceIdSet,
        HashSet<int> correctedServiceIds,
        StatsAccumulator stats
    )
    {
        List<(TrashIdMapping Mapping, StateRepairDetail Detail)> preserved = [];
        List<StateRepairDetail> removed = [];

        foreach (
            var entry in existingMappings.Values.Where(e => !configuredTrashIds.Contains(e.TrashId))
        )
        {
            // Service ID was corrected (trash_id fixed) - already handled in ProcessMatches
            if (correctedServiceIds.Contains(entry.ServiceId))
            {
                continue;
            }

            if (serviceIdSet.Contains(entry.ServiceId))
            {
                stats.RecordPreserved();
                var detail = new StateRepairDetail(
                    entry.Name,
                    entry.TrashId,
                    CachedTrashId: null,
                    entry.ServiceId,
                    entry.ServiceId,
                    StateRepairState.Preserved
                );
                preserved.Add((entry, detail));
            }
            else
            {
                stats.RecordRemoved();
                var detail = new StateRepairDetail(
                    entry.Name,
                    entry.TrashId,
                    CachedTrashId: null,
                    ServiceId: null,
                    entry.ServiceId,
                    StateRepairState.Removed
                );
                removed.Add(detail);
            }
        }

        return (preserved, removed);
    }

    private void RenderResultTree(
        IResourceAdapter adapter,
        IStateRepairSettings settings,
        StateRepairResult result,
        int configuredCount
    )
    {
        // Build resource type label with count
        var resourceLabel = $"[bold]{adapter.ResourceTypeName}[/] [dim]({configuredCount})[/]";
        var tree = new Tree(resourceLabel).Style(Style.Plain);

        // Group details by state
        var groupedByState = result
            .Details.GroupBy(d => d.State)
            .OrderBy(g => GetStatePriority(g.Key))
            .ToList();

        foreach (var stateGroup in groupedByState)
        {
            var stateConfig = GetStateConfig(stateGroup.Key);
            var count = stateGroup.Count();

            // Build state label with count and optional hint
            var stateLabel = stateConfig.Hint is not null
                ? $"[{stateConfig.Color}]{stateConfig.Label}[/] [dim]({count} - {stateConfig.Hint})[/]"
                : $"[{stateConfig.Color}]{stateConfig.Label}[/] [dim]({count})[/]";

            var stateNode = tree.AddNode(stateLabel);

            // In verbose mode, add individual items under each state
            if (settings.Verbose)
            {
                foreach (
                    var detail in stateGroup.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var itemLabel = FormatDetailNode(detail, stateConfig.Color);
                    stateNode.AddNode(itemLabel);
                }
            }
        }

        console.Write(tree);
        console.WriteLine();

        // Show state file path in verbose mode
        if (settings.Verbose)
        {
            console.MarkupLine($"[dim]State: {Markup.Escape(adapter.GetStateFilePath())}[/]");
            console.WriteLine();
        }
    }

    private static string FormatDetailNode(StateRepairDetail detail, Color stateColor)
    {
        var mapping = FormatMapping(detail);
        var name = Markup.Escape(detail.Name);
        return $"[{stateColor}]{name}[/] [dim]{mapping}[/]";
    }

    private static string FormatMapping(StateRepairDetail detail)
    {
        var trashIdPart = FormatTrashIdPart(detail.TrashId, detail.CachedTrashId);
        var serviceIdPart = FormatServiceIdPart(detail.ServiceId, detail.CachedServiceId);

        return (serviceIdPart, trashIdPart) switch
        {
            (null, _) => $"({trashIdPart})",
            (_, _) => $"({trashIdPart} -> {serviceIdPart})",
        };
    }

    private static string FormatTrashIdPart(string trashId, string? cachedTrashId)
    {
        var truncated = TruncateTrashId(trashId);

        if (cachedTrashId is null)
        {
            return truncated;
        }

        // Trash ID correction: show [oldId → newId]
        var cachedTruncated = TruncateTrashId(cachedTrashId);
        return $"[[[strikethrough]{cachedTruncated}[/] -> {truncated}]]";
    }

    private static string? FormatServiceIdPart(int? serviceId, int? cachedServiceId)
    {
        return (serviceId, cachedServiceId) switch
        {
            (null, null) => null,
            (null, { } cached) => $"[[[strikethrough]{cached}[/]]]",
            ({ } svcId, null) => svcId.ToString(CultureInfo.InvariantCulture),
            ({ } svcId, { } cached) when svcId == cached => svcId.ToString(
                CultureInfo.InvariantCulture
            ),
            ({ } svcId, { } cached) => $"[[[strikethrough]{cached}[/] -> {svcId}]]",
        };
    }

    private static string TruncateTrashId(string trashId)
    {
        const int maxLength = 8;
        return trashId.Length > maxLength ? trashId[..maxLength] + "..." : trashId;
    }

    private static int GetStatePriority(StateRepairState state) =>
        StateConfig.TryGetValue(state, out var cfg) ? cfg.Priority : 99;

    private static StateDisplayConfig GetStateConfig(StateRepairState state) =>
        StateConfig.TryGetValue(state, out var cfg)
            ? cfg
            : new StateDisplayConfig(99, Color.White, state.ToString(), null);

    private void ReportAmbiguousErrors(
        IResourceAdapter adapter,
        IReadOnlyList<AmbiguousMatch> ambiguous
    )
    {
        var table = new Table()
            .AddColumn("Guide Resource")
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
            .Header($"[red]Ambiguous {adapter.ResourceTypeName} Names[/]")
            .BorderColor(Color.Red);

        console.Write(panel);
        console.WriteLine();
        console.MarkupLine(
            $"[dim]Resolution: Delete or rename duplicates in {config.ServiceType}, then retry.[/]"
        );
        console.MarkupLine("[red]State NOT saved for this resource type.[/]");
    }
}
