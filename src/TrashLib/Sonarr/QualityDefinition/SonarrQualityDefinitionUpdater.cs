using System.Text.RegularExpressions;
using CliFx.Infrastructure;
using Serilog;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Api.Objects;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.QualityDefinition;

internal class SonarrQualityDefinitionUpdater : ISonarrQualityDefinitionUpdater
{
    private readonly ISonarrApi _api;
    private readonly IConsole _console;
    private readonly ISonarrQualityDefinitionGuideParser _parser;
    private readonly Regex _regexHybrid = new(@"720|1080", RegexOptions.Compiled);

    public SonarrQualityDefinitionUpdater(
        ILogger logger,
        ISonarrQualityDefinitionGuideParser parser,
        ISonarrApi api,
        IConsole console)
    {
        Log = logger;
        _parser = parser;
        _api = api;
        _console = console;
    }

    private ILogger Log { get; }

    public async Task Process(bool isPreview, SonarrConfiguration config)
    {
        Log.Information("Processing Quality Definition: {QualityDefinition}", config.QualityDefinition);
        var qualityDefinitions = _parser.ParseMarkdown(await _parser.GetMarkdownData());
        List<SonarrQualityData> selectedQuality;

        if (config.QualityDefinition == SonarrQualityDefinitionType.Hybrid)
        {
            selectedQuality = BuildHybridQuality(qualityDefinitions[SonarrQualityDefinitionType.Anime],
                qualityDefinitions[SonarrQualityDefinitionType.Series]);
        }
        else
        {
            selectedQuality = qualityDefinitions[config.QualityDefinition!.Value];
        }

        if (isPreview)
        {
            PrintQualityPreview(selectedQuality);
            return;
        }

        await ProcessQualityDefinition(selectedQuality);
    }

    private List<SonarrQualityData> BuildHybridQuality(IReadOnlyCollection<SonarrQualityData> anime,
        IEnumerable<SonarrQualityData> series)
    {
        // todo Verify anime & series are the same length? Probably not, because we might not care about some rows anyway.
        Log.Information(
            "Notice: Hybrid only functions on 720/1080 qualities and uses non-anime values for the rest (e.g. 2160)");

        var hybrid = new List<SonarrQualityData>();
        foreach (var left in series)
        {
            // Any qualities that anime doesn't care about get immediately added from Series quality
            var match = _regexHybrid.Match(left.Name);
            if (!match.Success)
            {
                Log.Debug("Using 'Series' Quality For: {QualityName}", left.Name);
                hybrid.Add(left);
                continue;
            }

            // If there's a quality in Series that Anime doesn't know about, we add the Series quality
            var right = anime.FirstOrDefault(row => row.Name == left.Name);
            if (right == null)
            {
                Log.Error("Could not find matching anime quality for series quality named {QualityName}",
                    left.Name);
                hybrid.Add(left);
                continue;
            }

            hybrid.Add(new SonarrQualityData
            {
                Name = left.Name,
                Min = Math.Min(left.Min, right.Min),
                Max = Math.Max(left.Max, right.Max)
            });
        }

        return hybrid;
    }

    private void PrintQualityPreview(IEnumerable<SonarrQualityData> quality)
    {
        _console.Output.WriteLine("");
        const string format = "{0,-20} {1,-10} {2,-15}";
        _console.Output.WriteLine(format, "Quality", "Min", "Max");
        _console.Output.WriteLine(format, "-------", "---", "---");

        foreach (var q in quality)
        {
            _console.Output.WriteLine(format, q.Name, q.AnnotatedMin, q.AnnotatedMax);
        }

        _console.Output.WriteLine("");
    }

    private async Task ProcessQualityDefinition(IEnumerable<SonarrQualityData> guideQuality)
    {
        var serverQuality = await _api.GetQualityDefinition();
        await UpdateQualityDefinition(serverQuality, guideQuality);
    }

    private async Task UpdateQualityDefinition(IReadOnlyCollection<SonarrQualityDefinitionItem> serverQuality,
        IEnumerable<SonarrQualityData> guideQuality)
    {
        static bool QualityIsDifferent(SonarrQualityDefinitionItem a, SonarrQualityData b)
        {
            return b.IsMinDifferent(a.MinSize) ||
                   b.IsMaxDifferent(a.MaxSize);
        }

        var newQuality = new List<SonarrQualityDefinitionItem>();
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
            newQuality.Add(entry);

            Log.Debug("Setting Quality " +
                      "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}]",
                entry.Quality?.Name, entry.Quality?.Source, entry.MinSize, entry.MaxSize);
        }

        await _api.UpdateQualityDefinition(newQuality);
        Log.Information("Number of updated qualities: {Count}", newQuality.Count);
    }
}
