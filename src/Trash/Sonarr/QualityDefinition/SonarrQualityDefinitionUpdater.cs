using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;
using Trash.Sonarr.Api;
using Trash.Sonarr.Api.Objects;

namespace Trash.Sonarr.QualityDefinition
{
    public class SonarrQualityDefinitionUpdater
    {
        private readonly ISonarrApi _api;
        private readonly ISonarrQualityDefinitionGuideParser _parser;
        private readonly Regex _regexHybrid = new(@"720|1080", RegexOptions.Compiled);

        public SonarrQualityDefinitionUpdater(ILogger logger, ISonarrQualityDefinitionGuideParser parser,
            ISonarrApi api)
        {
            Log = logger;
            _parser = parser;
            _api = api;
        }

        private ILogger Log { get; }

        private List<SonarrQualityData> BuildHybridQuality(List<SonarrQualityData> anime,
            List<SonarrQualityData> series)
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

        private static void PrintQualityPreview(IEnumerable<SonarrQualityData> quality)
        {
            Console.WriteLine("");
            const string format = "{0,-20} {1,-10} {2,-10}";
            Console.WriteLine(format, "Quality", "Min", "Max");
            Console.WriteLine(format, "-------", "---", "---");

            foreach (var q in quality)
            {
                Console.WriteLine(format, q.Name, q.Min, q.Max);
            }

            Console.WriteLine("");
        }

        public async Task Process(ISonarrCommand args, SonarrConfiguration config)
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

            if (args.Preview)
            {
                PrintQualityPreview(selectedQuality);
                return;
            }

            await ProcessQualityDefinition(selectedQuality);
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
                const decimal tolerance = 0.1m;
                return
                    Math.Abs(a.MaxSize - b.Max) > tolerance ||
                    Math.Abs(a.MinSize - b.Min) > tolerance;
            }

            // var newQuality = serverQuality.Where(q => guideQuality.Any(gq => gq.Name == q.Quality.Name));
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

                // Not using the original list again, so it's OK to modify the definition reftype objects in-place.
                entry.MinSize = qualityData.Min;
                entry.MaxSize = qualityData.Max;
                newQuality.Add(entry);

                Log.Debug("Setting Quality [Name: {Name}] [Min: {Min}] [Max: {Max}]",
                    entry.Quality?.Name, entry.MinSize, entry.MaxSize);
            }

            await _api.UpdateQualityDefinition(newQuality);
            Log.Information("Number of updated qualities: {Count}", newQuality.Count);
        }
    }
}
