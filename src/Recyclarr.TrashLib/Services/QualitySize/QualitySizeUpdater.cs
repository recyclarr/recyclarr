using CliFx.Infrastructure;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.QualitySize.Api;
using Serilog;

namespace Recyclarr.TrashLib.Services.QualitySize;

internal class QualitySizeUpdater : IQualitySizeUpdater
{
    private readonly ILogger _log;
    private readonly IQualityDefinitionService _api;
    private readonly IConsole _console;

    public QualitySizeUpdater(
        ILogger logger,
        IQualityDefinitionService api,
        IConsole console)
    {
        _log = logger;
        _api = api;
        _console = console;
    }

    public async Task Process(bool isPreview, QualityDefinitionConfig config, IGuideService guideService)
    {
        _log.Information("Processing Quality Definition: {QualityDefinition}", config.Type);
        var qualityDefinitions = guideService.GetQualities();
        var qualityTypeInConfig = config.Type;

        var selectedQuality = qualityDefinitions
            .FirstOrDefault(x => x.Type.EqualsIgnoreCase(qualityTypeInConfig));

        if (selectedQuality == null)
        {
            _log.Error("The specified quality definition type does not exist: {Type}", qualityTypeInConfig);
            return;
        }

        if (config.PreferredRatio is not null)
        {
            _log.Information("Using an explicit preferred ratio which will override values from the guide");

            // Fix an out of range ratio and warn the user
            if (config.PreferredRatio is < 0 or > 1)
            {
                var clampedRatio = Math.Clamp(config.PreferredRatio.Value, 0, 1);
                _log.Warning("Your `preferred_ratio` of {CurrentRatio} is out of range. " +
                    "It must be a decimal between 0.0 and 1.0. It has been clamped to {ClampedRatio}",
                    config.PreferredRatio, clampedRatio);

                config.PreferredRatio = clampedRatio;
            }

            // Apply a calculated preferred size
            foreach (var quality in selectedQuality.Qualities)
            {
                quality.Preferred = quality.InterpolatedPreferred(config.PreferredRatio.Value);
            }
        }

        if (isPreview)
        {
            PrintQualityPreview(selectedQuality.Qualities);
            return;
        }

        await ProcessQualityDefinition(selectedQuality.Qualities);
    }

    private void PrintQualityPreview(IReadOnlyCollection<QualitySizeItem> quality)
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

    private static bool QualityIsDifferent(ServiceQualityDefinitionItem a, QualitySizeItem b)
    {
        return b.IsMinDifferent(a.MinSize) || b.IsMaxDifferent(a.MaxSize) ||
            a.PreferredSize is not null && b.IsPreferredDifferent(a.PreferredSize);
    }

    private async Task ProcessQualityDefinition(IReadOnlyCollection<QualitySizeItem> guideQuality)
    {
        var serverQuality = await _api.GetQualityDefinition();

        var newQuality = new List<ServiceQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var serverEntry = serverQuality.FirstOrDefault(q => q.Quality?.Name == qualityData.Quality);
            if (serverEntry == null)
            {
                _log.Warning("Server lacks quality definition for {Quality}; it will be skipped", qualityData.Quality);
                continue;
            }

            if (!QualityIsDifferent(serverEntry, qualityData))
            {
                continue;
            }

            // Not using the original list again, so it's OK to modify the definition ref type objects in-place.
            serverEntry.MinSize = qualityData.MinForApi;
            serverEntry.MaxSize = qualityData.MaxForApi;
            serverEntry.PreferredSize = qualityData.PreferredForApi;
            newQuality.Add(serverEntry);

            _log.Debug("Setting Quality " +
                "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}] [Preferred: {Preferred}]",
                serverEntry.Quality?.Name, serverEntry.Quality?.Source, serverEntry.MinSize, serverEntry.MaxSize,
                serverEntry.PreferredSize);
        }

        await _api.UpdateQualityDefinition(newQuality);
        _log.Information("Number of updated qualities: {Count}", newQuality.Count);
    }
}
