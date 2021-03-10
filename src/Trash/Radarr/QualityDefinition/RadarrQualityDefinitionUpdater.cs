using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Trash.Radarr.Api;
using Trash.Radarr.Api.Objects;

namespace Trash.Radarr.QualityDefinition
{
    public class RadarrQualityDefinitionUpdater
    {
        private readonly IRadarrApi _api;
        private readonly IRadarrQualityDefinitionGuideParser _parser;

        public RadarrQualityDefinitionUpdater(IRadarrQualityDefinitionGuideParser parser, IRadarrApi api)
        {
            _parser = parser;
            _api = api;
        }

        private static void PrintQualityPreview(IEnumerable<RadarrQualityData> quality)
        {
            Console.WriteLine("");
            const string format = "{0,-20} {1,-10} {2,-10} {3,-10}";
            Console.WriteLine(format, "Quality", "Min", "Max", "Preferred");
            Console.WriteLine(format, "-------", "---", "---", "---------");

            foreach (var q in quality)
            {
                Console.WriteLine(format, q.Name, q.Min, q.Max, q.Preferred);
            }

            Console.WriteLine("");
        }

        public async Task Process(IRadarrCommand args, RadarrConfiguration config)
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
                quality.Preferred =
                    Math.Round(quality.Min + (quality.Max - quality.Min) * config.QualityDefinition.PreferredRatio, 1);
            }

            if (args.Preview)
            {
                PrintQualityPreview(selectedQuality);
                return;
            }

            await ProcessQualityDefinition(selectedQuality);
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
                const decimal tolerance = 0.1m;
                return
                    Math.Abs(a.MaxSize - b.Max) > tolerance ||
                    Math.Abs(a.MinSize - b.Min) > tolerance ||
                    Math.Abs(a.PreferredSize - b.Preferred) > tolerance;
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

                // Not using the original list again, so it's OK to modify the definition reftype objects in-place.
                entry.MinSize = qualityData.Min;
                entry.MaxSize = qualityData.Max;
                entry.PreferredSize = qualityData.Preferred;
                newQuality.Add(entry);

                Log.Debug("Setting Quality " +
                          "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}] [Preferred: {Preferred}]",
                    entry.Quality?.Name, entry.Quality?.Source, entry.MinSize, entry.MaxSize, entry.PreferredSize);
            }

            await _api.UpdateQualityDefinition(newQuality);
            Log.Information("Number of updated qualities: {Count}", newQuality.Count);
        }
    }
}
