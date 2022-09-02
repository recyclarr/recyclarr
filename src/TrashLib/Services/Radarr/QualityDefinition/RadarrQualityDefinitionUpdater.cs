using CliFx.Infrastructure;
using Common.Extensions;
using Serilog;
using TrashLib.Services.Radarr.Config;
using TrashLib.Services.Radarr.QualityDefinition.Api;
using TrashLib.Services.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Services.Radarr.QualityDefinition;

internal class RadarrQualityDefinitionUpdater : IRadarrQualityDefinitionUpdater
{
    private readonly ILogger _log;
    private readonly IQualityDefinitionService _api;
    private readonly IConsole _console;
    private readonly IRadarrGuideService _guide;

    public RadarrQualityDefinitionUpdater(
        ILogger logger,
        IRadarrGuideService guide,
        IQualityDefinitionService api,
        IConsole console)
    {
        _log = logger;
        _guide = guide;
        _api = api;
        _console = console;
    }

    public async Task Process(bool isPreview, RadarrConfiguration config)
    {
        _log.Information("Processing Quality Definition: {QualityDefinition}", config.QualityDefinition!.Type);
        var qualityDefinitions = _guide.GetQualities();
        var qualityTypeInConfig = config.QualityDefinition!.Type;

        var selectedQuality = qualityDefinitions
            .FirstOrDefault(x => x.Type.EqualsIgnoreCase(qualityTypeInConfig));

        if (selectedQuality == null)
        {
            _log.Error("The specified quality definition type does not exist: {Type}", qualityTypeInConfig);
            return;
        }

        // Fix an out of range ratio and warn the user
        if (config.QualityDefinition.PreferredRatio is < 0 or > 1)
        {
            var clampedRatio = Math.Clamp(config.QualityDefinition.PreferredRatio, 0, 1);
            _log.Warning("Your `preferred_ratio` of {CurrentRatio} is out of range. " +
                         "It must be a decimal between 0.0 and 1.0. It has been clamped to {ClampedRatio}",
                config.QualityDefinition.PreferredRatio, clampedRatio);

            config.QualityDefinition.PreferredRatio = clampedRatio;
        }

        // Apply a calculated preferred size
        foreach (var quality in selectedQuality.Qualities)
        {
            quality.Preferred = quality.InterpolatedPreferred(config.QualityDefinition.PreferredRatio);
        }

        if (isPreview)
        {
            PrintQualityPreview(selectedQuality.Qualities);
            return;
        }

        await ProcessQualityDefinition(selectedQuality.Qualities);
    }

    private void PrintQualityPreview(IEnumerable<RadarrQualityItem> quality)
    {
        _console.Output.WriteLine("");
        const string format = "{0,-20} {1,-10} {2,-15} {3,-15}";
        _console.Output.WriteLine(format, "Quality", "Min", "Max", "Preferred");
        _console.Output.WriteLine(format, "-------", "---", "---", "---------");

        foreach (var q in quality)
        {
            _console.Output.WriteLine(format, q.Quality, q.AnnotatedMin, q.AnnotatedMax, q.AnnotatedPreferred);
        }

        _console.Output.WriteLine("");
    }

    private async Task ProcessQualityDefinition(IEnumerable<RadarrQualityItem> guideQuality)
    {
        var serverQuality = await _api.GetQualityDefinition();
        await UpdateQualityDefinition(serverQuality, guideQuality);
    }

    private async Task UpdateQualityDefinition(IReadOnlyCollection<RadarrQualityDefinitionItem> serverQuality,
        IEnumerable<RadarrQualityItem> guideQuality)
    {
        static bool QualityIsDifferent(RadarrQualityDefinitionItem a, RadarrQualityItem b)
        {
            return b.IsMinDifferent(a.MinSize) ||
                   b.IsMaxDifferent(a.MaxSize) ||
                   b.IsPreferredDifferent(a.PreferredSize);
        }

        var newQuality = new List<RadarrQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var entry = serverQuality.FirstOrDefault(q => q.Quality?.Name == qualityData.Quality);
            if (entry == null)
            {
                _log.Warning("Server lacks quality definition for {Quality}; it will be skipped", qualityData.Quality);
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

            _log.Debug("Setting Quality " +
                       "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}] [Preferred: {Preferred}]",
                entry.Quality?.Name, entry.Quality?.Source, entry.MinSize, entry.MaxSize, entry.PreferredSize);
        }

        await _api.UpdateQualityDefinition(newQuality);
        _log.Information("Number of updated qualities: {Count}", newQuality.Count);
    }
}
