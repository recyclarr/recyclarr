using CliFx.Infrastructure;
using Common.Extensions;
using Serilog;
using TrashLib.Extensions;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Processors;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace TrashLib.Radarr.CustomFormat;

internal class CustomFormatUpdater : ICustomFormatUpdater
{
    private readonly ICachePersister _cache;
    private readonly IGuideProcessor _guideProcessor;
    private readonly IPersistenceProcessor _persistenceProcessor;
    private readonly IConsole _console;

    public CustomFormatUpdater(
        ILogger log,
        ICachePersister cache,
        IGuideProcessor guideProcessor,
        IPersistenceProcessor persistenceProcessor,
        IConsole console)
    {
        Log = log;
        _cache = cache;
        _guideProcessor = guideProcessor;
        _persistenceProcessor = persistenceProcessor;
        _console = console;
    }

    private ILogger Log { get; }

    public async Task Process(bool isPreview, RadarrConfiguration config)
    {
        _cache.Load();

        await _guideProcessor.BuildGuideDataAsync(config.CustomFormats.AsReadOnly(), _cache.CfCache);

        if (!ValidateGuideDataAndCheckShouldProceed(config))
        {
            return;
        }

        if (isPreview)
        {
            PreviewCustomFormats();
        }
        else
        {
            await _persistenceProcessor.PersistCustomFormats(_guideProcessor.ProcessedCustomFormats,
                _guideProcessor.DeletedCustomFormatsInCache, _guideProcessor.ProfileScores);

            PrintApiStatistics(_persistenceProcessor.Transactions);
            PrintQualityProfileUpdates();

            // Cache all the custom formats (using ID from API response).
            _cache.Update(_guideProcessor.ProcessedCustomFormats);
            _cache.Save();
        }

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

                foreach (var (customFormatName, score, reason) in scores)
                {
                    Log.Debug("  - {Format}: {Score} ({Reason})", customFormatName, score, reason);
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

    private void PrintApiStatistics(CustomFormatTransactionData transactions)
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

        var skipped = transactions.UnchangedCustomFormats;
        if (skipped.Count > 0)
        {
            Log.Debug("Skipped {Count} Custom Formats that did not change: {CustomFormats}", skipped.Count,
                skipped.Select(r => r.Name));
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
        _console.Output.WriteLine("");

        if (_guideProcessor.DuplicatedCustomFormats.Count > 0)
        {
            Log.Warning("One or more of the custom formats you want are duplicated in the guide. These custom " +
                        "formats WILL BE SKIPPED. Recyclarr is not able to choose which one you actually " +
                        "wanted. To resolve this ambiguity, use the `trash_ids` property in your YML " +
                        "configuration to refer to the custom format using its Trash ID instead of its name");

            foreach (var (cfName, dupes) in _guideProcessor.DuplicatedCustomFormats)
            {
                Log.Warning("{CfName} is duplicated {DupeTimes} with the following Trash IDs:", cfName, dupes.Count);
                foreach (var cf in dupes)
                {
                    Log.Warning(" - {TrashId}", cf.TrashId);
                }
            }

            _console.Output.WriteLine("");
        }

        if (_guideProcessor.CustomFormatsNotInGuide.Count > 0)
        {
            Log.Warning("The Custom Formats below do not exist in the guide and will " +
                        "be skipped. Names must match the 'name' field in the actual JSON, not the header in " +
                        "the guide! Either fix the names or remove them from your YAML config to resolve this " +
                        "warning");
            Log.Warning("{CfList}", _guideProcessor.CustomFormatsNotInGuide);

            _console.Output.WriteLine("");
        }

        var cfsWithoutQualityProfiles = _guideProcessor.ConfigData
            .Where(d => d.QualityProfiles.Count == 0)
            .SelectMany(d => d.CustomFormats.Select(cf => cf.Name))
            .ToList();

        if (cfsWithoutQualityProfiles.Count > 0)
        {
            Log.Debug("These custom formats will be uploaded but are not associated to a quality profile in the " +
                      "config file: {UnassociatedCfs}", cfsWithoutQualityProfiles);

            _console.Output.WriteLine("");
        }

        // No CFs are defined in this item, or they are all invalid. Skip this whole instance.
        if (_guideProcessor.ConfigData.Count == 0)
        {
            Log.Error("Guide processing yielded no custom formats for configured instance host {BaseUrl}",
                FlurlLogging.SanitizeUrl(config.BaseUrl));
            return false;
        }

        if (_guideProcessor.CustomFormatsWithoutScore.Count > 0)
        {
            Log.Information("The below custom formats have no score in the guide or in your YAML config. They will " +
                            "still be synced to Radarr, but no score will be set in your chosen quality profiles");
            foreach (var tuple in _guideProcessor.CustomFormatsWithoutScore)
            {
                Log.Information("{CfList}", tuple);
            }

            _console.Output.WriteLine("");
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

            _console.Output.WriteLine("");
        }

        return true;
    }

    private void PreviewCustomFormats()
    {
        _console.Output.WriteLine("");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("            >>> Custom Formats From Guide <<<            ");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("");

        const string format = "{0,-30} {1,-35}";
        _console.Output.WriteLine(format, "Custom Format", "Trash ID");
        _console.Output.WriteLine(string.Concat(Enumerable.Repeat('-', 1 + 30 + 35)));

        foreach (var cf in _guideProcessor.ProcessedCustomFormats)
        {
            _console.Output.WriteLine(format, cf.Name, cf.TrashId);
        }

        _console.Output.WriteLine("");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("      >>> Quality Profile Assignments & Scores <<<       ");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("");

        const string profileFormat = "{0,-18} {1,-20} {2,-8}";
        _console.Output.WriteLine(profileFormat, "Profile", "Custom Format", "Score");
        _console.Output.WriteLine(string.Concat(Enumerable.Repeat('-', 2 + 18 + 20 + 8)));

        foreach (var (profileName, scoreMap) in _guideProcessor.ProfileScores)
        {
            _console.Output.WriteLine(profileFormat, profileName, "", "");

            foreach (var (customFormat, score) in scoreMap.Mapping)
            {
                var matchingCf = _guideProcessor.ProcessedCustomFormats
                    .FirstOrDefault(cf => cf.TrashId.EqualsIgnoreCase(customFormat.TrashId));

                if (matchingCf == null)
                {
                    Log.Warning("Quality Profile refers to CF not found in guide: {TrashId}",
                        customFormat.TrashId);
                    continue;
                }

                _console.Output.WriteLine(profileFormat, "", matchingCf.Name, score);
            }
        }

        _console.Output.WriteLine("");
    }
}
