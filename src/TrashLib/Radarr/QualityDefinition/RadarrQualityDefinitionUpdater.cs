using CliFx.Infrastructure;
using Serilog;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.QualityDefinition.Api;
using TrashLib.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Radarr.QualityDefinition;

internal class RadarrQualityDefinitionUpdater : IRadarrQualityDefinitionUpdater
{
    private readonly IQualityDefinitionService _api;
    private readonly IConsole _console;
    private readonly IRadarrQualityDefinitionGuideParser _parser;

    public RadarrQualityDefinitionUpdater(
        ILogger logger,
        IRadarrQualityDefinitionGuideParser parser,
        IQualityDefinitionService api,
        IConsole console)
    {
        Log = logger;
        _parser = parser;
        _api = api;
        _console = console;
    }

    private ILogger Log { get; }

    public async Task Process(bool isPreview, RadarrConfiguration config)
    {
        Log.Information("Processing Quality Definition: {QualityDefinition}", config.QualityDefinition!.Type);
        var qualityDefinitions = _parser.ParseMarkdown(await _parser.GetMarkdownData());

        var selectedQuality = qualityDefinitions[config.QualityDefinition!.Type];

        // Fix an out of range ratio and warn the user
        if (config.QualityDefinition.PreferredRatio is < 0 or > 1)
        {
            var clampedRatio = Math.Clamp(config.QualityDefinition.PreferredRatio, 0, 1);
            Log.Warning("Your `preferred_ratio` of {CurrentRatio} is out of range. " +
                        "It must be a decimal between 0.0 and 1.0. It has been clamped to {ClampedRatio}",
                config.QualityDefinition.PreferredRatio, clampedRatio);

            config.QualityDefinition.PreferredRatio = clampedRatio;
        }

        // Apply a calculated preferred size
        foreach (var quality in selectedQuality)
        {
            quality.Preferred = quality.InterpolatedPreferred(config.QualityDefinition.PreferredRatio);
        }

        if (isPreview)
        {
            PrintQualityPreview(selectedQuality);
            return;
        }

        await ProcessQualityDefinition(selectedQuality);
    }

    private void PrintQualityPreview(IEnumerable<RadarrQualityData> quality)
    {
        _console.Output.WriteLine("");
        const string format = "{0,-20} {1,-10} {2,-15} {3,-15}";
        _console.Output.WriteLine(format, "Quality", "Min", "Max", "Preferred");
        _console.Output.WriteLine(format, "-------", "---", "---", "---------");

        foreach (var q in quality)
        {
            _console.Output.WriteLine(format, q.Name, q.AnnotatedMin, q.AnnotatedMax, q.AnnotatedPreferred);
        }

        _console.Output.WriteLine("");
    }

    private async Task ProcessQualityDefinition(IEnumerable<RadarrQualityData> guideQuality)
    {
        var serverQuality = await _api.GetQualityDefinition();
        await UpdateQualityDefinition(serverQuality, guideQuality);
    }

    private async Task UpdateQualityDefinition(IReadOnlyCollection<RadarrQualityDefinitionItem> serverQuality,
        IEnumerable<RadarrQualityData> guideQuality)
    {
        static bool QualityIsDifferent(RadarrQualityDefinitionItem a, RadarrQualityData b)
        {
            return b.IsMinDifferent(a.MinSize) ||
                   b.IsMaxDifferent(a.MaxSize) ||
                   b.IsPreferredDifferent(a.PreferredSize);
        }

        var newQuality = new List<RadarrQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var entry = serverQuality.FirstOrDefault(q => q.Quality?.Name == qualityData.Name);
            if (entry == null)
            {
                Log.Warning("Server lacks quality definition for {Quality}; it will be skipped", qualityData.Name);
                continue;
            }

            if (!QualityIsDifferent(entry, qualityData))
            {
                continue;
            }

            // Not using the original list again, so it's OK to modify the definition ref type objects in-place.
            entry.MinSize = qualityData.MinForApi;
            entry.MaxSize = qualityData.MaxForApi;
            entry.PreferredSize = qualityData.PreferredForApi;
            newQuality.Add(entry);

            Log.Debug("Setting Quality " +
                      "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}] [Preferred: {Preferred}]",
                entry.Quality?.Name, entry.Quality?.Source, entry.MinSize, entry.MaxSize, entry.PreferredSize);
        }

        await _api.UpdateQualityDefinition(newQuality);
        Log.Information("Number of updated qualities: {Count}", newQuality.Count);
    }
}
