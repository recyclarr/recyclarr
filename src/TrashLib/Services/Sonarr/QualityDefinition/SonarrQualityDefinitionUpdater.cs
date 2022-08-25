using System.Text.RegularExpressions;
using CliFx.Infrastructure;
using Common.Extensions;
using Serilog;
using TrashLib.Services.Common.QualityDefinition;
using TrashLib.Services.Sonarr.Api;
using TrashLib.Services.Sonarr.Api.Objects;
using TrashLib.Services.Sonarr.Config;
using TrashLib.Services.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Services.Sonarr.QualityDefinition;

internal class SonarrQualityDefinitionUpdater : ISonarrQualityDefinitionUpdater
{
    private readonly ILogger _log;
    private readonly ISonarrApi _api;
    private readonly IConsole _console;
    private readonly ISonarrGuideService _guide;
    private readonly Regex _regexHybrid = new(@"720|1080", RegexOptions.Compiled);

    public SonarrQualityDefinitionUpdater(
        ILogger logger,
        ISonarrGuideService guide,
        ISonarrApi api,
        IConsole console)
    {
        _log = logger;
        _guide = guide;
        _api = api;
        _console = console;
    }

    private SonarrQualityData? GetQualityOrError(ICollection<SonarrQualityData> qualityDefinitions, string type)
    {
        var quality = qualityDefinitions.FirstOrDefault(x => x.Type.EqualsIgnoreCase(type));
        if (quality is null)
        {
            _log.Error(
                "The following quality definition is required for hybrid, but was not found in the guide: {Type}",
                type);
        }

        return quality;
    }

    public async Task Process(bool isPreview, SonarrConfiguration config)
    {
        _log.Information("Processing Quality Definition: {QualityDefinition}", config.QualityDefinition);
        var qualityDefinitions = _guide.GetQualities();
        var qualityTypeInConfig = config.QualityDefinition;

        // var qualityDefinitions = _parser.ParseMarkdown(await _parser.GetMarkdownData());
        SonarrQualityData? selectedQuality;

        if (config.QualityDefinition.EqualsIgnoreCase("hybrid"))
        {
            var animeQuality = GetQualityOrError(qualityDefinitions, "anime");
            var seriesQuality = GetQualityOrError(qualityDefinitions, "series");
            if (animeQuality is null || seriesQuality is null)
            {
                return;
            }

            selectedQuality = BuildHybridQuality(animeQuality.Qualities, seriesQuality.Qualities);
        }
        else
        {
            selectedQuality = qualityDefinitions
                .FirstOrDefault(x => x.Type.EqualsIgnoreCase(qualityTypeInConfig));

            if (selectedQuality == null)
            {
                _log.Error("The specified quality definition type does not exist: {Type}", qualityTypeInConfig);
                return;
            }
        }

        if (isPreview)
        {
            PrintQualityPreview(selectedQuality.Qualities);
            return;
        }

        await ProcessQualityDefinition(selectedQuality.Qualities);
    }

    private SonarrQualityData BuildHybridQuality(
        IReadOnlyCollection<QualityItem> anime,
        IReadOnlyCollection<QualityItem> series)
    {
        // todo Verify anime & series are the same length? Probably not, because we might not care about some rows anyway.
        _log.Information(
            "Notice: Hybrid only functions on 720/1080 qualities and uses non-anime values for the rest (e.g. 2160)");

        var hybrid = new List<QualityItem>();
        foreach (var left in series)
        {
            // Any qualities that anime doesn't care about get immediately added from Series quality
            var match = _regexHybrid.Match(left.Quality);
            if (!match.Success)
            {
                _log.Debug("Using 'Series' Quality For: {QualityName}", left.Quality);
                hybrid.Add(left);
                continue;
            }

            // If there's a quality in Series that Anime doesn't know about, we add the Series quality
            var right = anime.FirstOrDefault(row => row.Quality == left.Quality);
            if (right == null)
            {
                _log.Error("Could not find matching anime quality for series quality named {QualityName}",
                    left.Quality);
                hybrid.Add(left);
                continue;
            }

            hybrid.Add(new QualityItem(left.Quality,
                Math.Min(left.Min, right.Min),
                Math.Max(left.Max, right.Max)));
        }

        return new SonarrQualityData("", "hybrid", hybrid);
    }

    private void PrintQualityPreview(IEnumerable<QualityItem> quality)
    {
        _console.Output.WriteLine("");
        const string format = "{0,-20} {1,-10} {2,-15}";
        _console.Output.WriteLine(format, "Quality", "Min", "Max");
        _console.Output.WriteLine(format, "-------", "---", "---");

        foreach (var q in quality)
        {
            _console.Output.WriteLine(format, q.Quality, q.AnnotatedMin, q.AnnotatedMax);
        }

        _console.Output.WriteLine("");
    }

    private async Task ProcessQualityDefinition(IEnumerable<QualityItem> guideQuality)
    {
        var serverQuality = await _api.GetQualityDefinition();
        await UpdateQualityDefinition(serverQuality, guideQuality);
    }

    private async Task UpdateQualityDefinition(IReadOnlyCollection<SonarrQualityDefinitionItem> serverQuality,
        IEnumerable<QualityItem> guideQuality)
    {
        static bool QualityIsDifferent(SonarrQualityDefinitionItem a, QualityItem b)
        {
            return b.IsMinDifferent(a.MinSize) ||
                   b.IsMaxDifferent(a.MaxSize);
        }

        var newQuality = new List<SonarrQualityDefinitionItem>();
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
            newQuality.Add(entry);

            _log.Debug("Setting Quality " +
                       "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}]",
                entry.Quality?.Name, entry.Quality?.Source, entry.MinSize, entry.MaxSize);
        }

        await _api.UpdateQualityDefinition(newQuality);
        _log.Information("Number of updated qualities: {Count}", newQuality.Count);
    }
}
