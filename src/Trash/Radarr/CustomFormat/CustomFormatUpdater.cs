using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Trash.Command;
using Trash.Extensions;
using Trash.Radarr.CustomFormat.Processors;
using Trash.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace Trash.Radarr.CustomFormat
{
    internal class CustomFormatUpdater : ICustomFormatUpdater
    {
        private readonly ICachePersister _cache;
        private readonly IGuideProcessor _guideProcessor;
        private readonly IPersistenceProcessor _persistenceProcessor;

        public CustomFormatUpdater(
            ILogger log,
            ICachePersister cache,
            IGuideProcessor guideProcessor,
            IPersistenceProcessor persistenceProcessor)
        {
            Log = log;
            _cache = cache;
            _guideProcessor = guideProcessor;
            _persistenceProcessor = persistenceProcessor;
        }

        private ILogger Log { get; }

        public async Task Process(IServiceCommand args, RadarrConfiguration config)
        {
            _cache.Load();

            await _guideProcessor.BuildGuideData(config.CustomFormats, _cache.CfCache);

            if (!ValidateGuideDataAndCheckShouldProceed(config))
            {
                return;
            }

            if (args.Preview)
            {
                PreviewCustomFormats();
                return;
            }

            await _persistenceProcessor.PersistCustomFormats(_guideProcessor.ProcessedCustomFormats,
                _guideProcessor.DeletedCustomFormatsInCache, _guideProcessor.ProfileScores);

            PrintApiStatistics(args, _persistenceProcessor.Transactions);
            PrintQualityProfileUpdates();

            // Cache all the custom formats (using ID from API response).
            _cache.Update(_guideProcessor.ProcessedCustomFormats);
            _cache.Save();

            _persistenceProcessor.Reset();
            _guideProcessor.Reset();
        }

        private void PrintQualityProfileUpdates()
        {
            if (_persistenceProcessor.UpdatedScores.Count > 0)
            {
                foreach (var (profileName, scores) in _persistenceProcessor.UpdatedScores)
                {
                    Log.Debug("> Scores updated for quality profile: {ProfileName}", profileName);

                    foreach (var score in scores)
                    {
                        Log.Debug("  - {Format}: {Score}", score.CustomFormat.Name, score.Score);
                    }
                }

                Log.Information("Updated {ProfileCount} profiles and a total of {ScoreCount} scores",
                    _persistenceProcessor.UpdatedScores.Keys.Count,
                    _persistenceProcessor.UpdatedScores.Sum(s => s.Value.Count));
            }
            else
            {
                Log.Information("All quality profile scores are already up to date!");
            }

            if (_persistenceProcessor.InvalidProfileNames.Count > 0)
            {
                Log.Warning("The following quality profile names are not valid and should either be " +
                            "removed or renamed in your YAML config");
                Log.Warning("{QualityProfileNames}", _persistenceProcessor.InvalidProfileNames);
            }
        }

        private void PrintApiStatistics(IServiceCommand args, CustomFormatTransactionData transactions)
        {
            var created = transactions.NewCustomFormats;
            if (created.Count > 0)
            {
                Log.Information("Created {Count} New Custom Formats: {CustomFormats}", created.Count,
                    created.Select(r => r.Name));
            }

            var updated = transactions.UpdatedCustomFormats;
            if (updated.Count > 0)
            {
                Log.Information("Updated {Count} Existing Custom Formats: {CustomFormats}", updated.Count,
                    updated.Select(r => r.Name));
            }

            if (args.Debug)
            {
                var skipped = transactions.UnchangedCustomFormats;
                if (skipped.Count > 0)
                {
                    Log.Debug("Skipped {Count} Custom Formats that did not change: {CustomFormats}", skipped.Count,
                        skipped.Select(r => r.Name));
                }
            }

            var deleted = transactions.DeletedCustomFormatIds;
            if (deleted.Count > 0)
            {
                Log.Information("Deleted {Count} Custom Formats: {CustomFormats}", deleted.Count,
                    deleted.Select(r => r.CustomFormatName));
            }

            var totalCount = created.Count + updated.Count;
            if (totalCount > 0)
            {
                Log.Information("Total of {Count} custom formats synced to Radarr", totalCount);
            }
            else
            {
                Log.Information("All custom formats are already up to date!");
            }
        }

        private bool ValidateGuideDataAndCheckShouldProceed(RadarrConfiguration config)
        {
            if (_guideProcessor.CustomFormatsNotInGuide.Count > 0)
            {
                Log.Warning("The Custom Formats below do not exist in the guide and will " +
                            "be skipped. Names must match the 'name' field in the actual JSON, not the header in " +
                            "the guide! Either fix the names or remove them from your YAML config to resolve this " +
                            "warning");
                Log.Warning("{CfList}", _guideProcessor.CustomFormatsNotInGuide);
            }

            var cfsWithoutQualityProfiles = _guideProcessor.ConfigData
                .Where(d => d.QualityProfiles.Count == 0)
                .SelectMany(d => d.CustomFormats.Select(cf => cf.Name))
                .ToList();

            if (cfsWithoutQualityProfiles.Count > 0)
            {
                Log.Debug("These custom formats will be uploaded but are not associated to a quality profile in the " +
                          "config file: {UnassociatedCfs}", cfsWithoutQualityProfiles);
            }

            // No CFs are defined in this item, or they are all invalid. Skip this whole instance.
            if (_guideProcessor.ConfigData.Count == 0)
            {
                Log.Error("Guide processing yielded no custom formats for configured instance host {BaseUrl}",
                    config.BaseUrl);
                return false;
            }

            if (_guideProcessor.CustomFormatsWithoutScore.Count > 0)
            {
                Log.Warning("The below custom formats have no score in the guide or YAML " +
                            "config and will be skipped (remove them from your config or specify a " +
                            "score to fix this warning)");
                foreach (var tuple in _guideProcessor.CustomFormatsWithoutScore)
                {
                    Log.Warning("{CfList}", tuple);
                }
            }

            if (_guideProcessor.CustomFormatsWithOutdatedNames.Count > 0)
            {
                Log.Warning("One or more custom format names in your YAML config have been renamed in the guide and " +
                            "are outdated. Each outdated name will be listed below. These custom formats will refuse " +
                            "to sync if your cache is deleted. To fix this warning, rename each one to its new name");

                foreach (var (oldName, newName) in _guideProcessor.CustomFormatsWithOutdatedNames)
                {
                    Log.Warning(" - '{OldName}' -> '{NewName}'", oldName, newName);
                }
            }

            return true;
        }

        private void PreviewCustomFormats()
        {
            Console.WriteLine("");
            Console.WriteLine("=========================================================");
            Console.WriteLine("            >>> Custom Formats From Guide <<<            ");
            Console.WriteLine("=========================================================");
            Console.WriteLine("");

            const string format = "{0,-30} {1,-35}";
            Console.WriteLine(format, "Custom Format", "Trash ID");
            Console.WriteLine(string.Concat(Enumerable.Repeat('-', 1 + 30 + 35)));

            foreach (var cf in _guideProcessor.ProcessedCustomFormats)
            {
                Console.WriteLine(format, cf.Name, cf.TrashId);
            }

            Console.WriteLine("");
            Console.WriteLine("=========================================================");
            Console.WriteLine("      >>> Quality Profile Assignments & Scores <<<       ");
            Console.WriteLine("=========================================================");
            Console.WriteLine("");

            const string profileFormat = "{0,-18} {1,-20} {2,-8}";
            Console.WriteLine(profileFormat, "Profile", "Custom Format", "Score");
            Console.WriteLine(string.Concat(Enumerable.Repeat('-', 2 + 18 + 20 + 8)));

            foreach (var (profileName, scoreEntries) in _guideProcessor.ProfileScores)
            {
                Console.WriteLine(profileFormat, profileName, "", "");

                foreach (var scoreEntry in scoreEntries)
                {
                    var matchingCf = _guideProcessor.ProcessedCustomFormats
                        .FirstOrDefault(cf => cf.TrashId.EqualsIgnoreCase(scoreEntry.CustomFormat.TrashId));

                    if (matchingCf == null)
                    {
                        Log.Warning("Quality Profile refers to CF not found in guide: {TrashId}",
                            scoreEntry.CustomFormat.TrashId);
                        continue;
                    }

                    Console.WriteLine(profileFormat, "", matchingCf.Name, scoreEntry.Score);
                }
            }

            Console.WriteLine("");
        }
    }
}
