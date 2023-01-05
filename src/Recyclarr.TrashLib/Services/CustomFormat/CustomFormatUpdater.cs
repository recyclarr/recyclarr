using CliFx.Infrastructure;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.CustomFormat.Processors;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;
using Serilog;

namespace Recyclarr.TrashLib.Services.CustomFormat;

internal class CustomFormatUpdater : ICustomFormatUpdater
{
    private readonly ICachePersister _cache;
    private readonly IGuideProcessor _guideProcessor;
    private readonly IPersistenceProcessor _persistenceProcessor;
    private readonly IConsole _console;
    private readonly IServiceRequestBuilder _service;
    private readonly ILogger _log;

    public CustomFormatUpdater(
        ILogger log,
        ICachePersister cache,
        IGuideProcessor guideProcessor,
        IPersistenceProcessor persistenceProcessor,
        IConsole console,
        IServiceRequestBuilder service)
    {
        _log = log;
        _cache = cache;
        _guideProcessor = guideProcessor;
        _persistenceProcessor = persistenceProcessor;
        _console = console;
        _service = service;
    }

    public async Task Process(bool isPreview, IEnumerable<CustomFormatConfig> configs, IGuideService guideService)
    {
        _cache.Load();

        await _guideProcessor.BuildGuideDataAsync(configs, _cache.CfCache, guideService);

        if (!ValidateGuideDataAndCheckShouldProceed())
        {
            return;
        }

        if (isPreview)
        {
            PreviewCustomFormats();
            return;
        }

        await _persistenceProcessor.PersistCustomFormats(
            _guideProcessor.ProcessedCustomFormats,
            _guideProcessor.DeletedCustomFormatsInCache,
            _guideProcessor.ProfileScores);

        PrintApiStatistics(_persistenceProcessor.Transactions);
        PrintQualityProfileUpdates();

        // Cache all the custom formats (using ID from API response).
        _cache.Update(_guideProcessor.ProcessedCustomFormats);
        _cache.Save();
    }

    private void PrintQualityProfileUpdates()
    {
        if (_persistenceProcessor.UpdatedScores.Count > 0)
        {
            foreach (var (profileName, scores) in _persistenceProcessor.UpdatedScores)
            {
                _log.Debug("> Scores updated for quality profile: {ProfileName}", profileName);

                foreach (var (customFormatName, score, reason) in scores)
                {
                    _log.Debug("  - {Format}: {Score} ({Reason})", customFormatName, score, reason);
                }
            }

            _log.Information("Updated {ProfileCount} profiles and a total of {ScoreCount} scores",
                _persistenceProcessor.UpdatedScores.Keys.Count,
                _persistenceProcessor.UpdatedScores.Sum(s => s.Value.Count));
        }
        else
        {
            _log.Information("All quality profile scores are already up to date!");
        }

        if (_persistenceProcessor.InvalidProfileNames.Count > 0)
        {
            _log.Warning("The following quality profile names are not valid and should either be " +
                "removed or renamed in your YAML config");
            _log.Warning("{QualityProfileNames}", _persistenceProcessor.InvalidProfileNames);
        }
    }

    private void PrintApiStatistics(CustomFormatTransactionData transactions)
    {
        foreach (var (guideCf, conflictingId) in transactions.ConflictingCustomFormats)
        {
            _log.Warning(
                "Custom Format with name {Name} (Trash ID: {TrashId}) will be skipped because another " +
                "CF already exists with that name (ID: {ConflictId}). To fix the conflict, delete or " +
                "rename the CF with the mentioned name",
                guideCf.Name, guideCf.TrashId, conflictingId);
        }

        var created = transactions.NewCustomFormats;
        if (created.Count > 0)
        {
            _log.Information("Created {Count} New Custom Formats", created.Count);
            _log.Debug("Custom formats Created: {CustomFormats}",
                created.ToDictionary(k => k.TrashId, v => v.Name));

            foreach (var mapping in created)
            {
                _console.Output.WriteLine($"> Created: {mapping.TrashId} ({mapping.Name})");
            }
        }

        var updated = transactions.UpdatedCustomFormats;
        if (updated.Count > 0)
        {
            _log.Information("Updated {Count} Existing Custom Formats", updated.Count);
            _log.Debug("Custom formats Updated: {CustomFormats}",
                updated.ToDictionary(k => k.TrashId, v => v.Name));

            foreach (var mapping in updated)
            {
                _console.Output.WriteLine($"> Updated: {mapping.TrashId} ({mapping.Name})");
            }
        }

        var skipped = transactions.UnchangedCustomFormats;
        if (skipped.Count > 0)
        {
            _log.Information("Skipped {Count} Custom Formats that did not change", skipped.Count);
            _log.Debug("Custom Formats Skipped: {CustomFormats}",
                skipped.ToDictionary(k => k.TrashId, v => v.Name));

            foreach (var mapping in skipped)
            {
                _console.Output.WriteLine($"> Skipped: {mapping.TrashId} ({mapping.Name})");
            }
        }

        var deleted = transactions.DeletedCustomFormatIds;
        if (deleted.Count > 0)
        {
            _log.Information("Deleted {Count} Custom Formats", deleted.Count);
            _log.Debug("Custom formats Deleted: {CustomFormats}",
                deleted.ToDictionary(k => k.TrashId, v => v.CustomFormatName));

            foreach (var mapping in deleted)
            {
                _console.Output.WriteLine($"> Deleted: {mapping.TrashId} ({mapping.CustomFormatName})");
            }
        }

        var totalCount = created.Count + updated.Count;
        if (totalCount > 0)
        {
            _log.Information("Total of {Count} custom formats were synced", totalCount);
        }
        else
        {
            _log.Information("All custom formats are already up to date!");
        }
    }

    private bool ValidateGuideDataAndCheckShouldProceed()
    {
        if (_guideProcessor.CustomFormatsNotInGuide.Count > 0)
        {
            _log.Warning(
                "The Custom Formats below do not exist in the guide and will " +
                "be skipped. Trash IDs must match what is listed in the output when using the " +
                "`--list-custom-formats` option");
            _log.Warning("{CfList}", _guideProcessor.CustomFormatsNotInGuide);

            _console.Output.WriteLine("");
        }

        var cfsWithoutQualityProfiles = _guideProcessor.ConfigData
            .Where(d => d.QualityProfiles.Count == 0)
            .SelectMany(d => d.CustomFormats.Select(cf => cf.Name))
            .ToList();

        if (cfsWithoutQualityProfiles.Count > 0)
        {
            _log.Debug(
                "These custom formats will be uploaded but are not associated to a quality profile in the " +
                "config file: {UnassociatedCfs}", cfsWithoutQualityProfiles);

            _console.Output.WriteLine("");
        }

        // No CFs are defined in this item, or they are all invalid. Skip this whole instance.
        if (_guideProcessor.ConfigData.Count == 0)
        {
            _log.Error("Guide processing yielded no custom formats for configured instance host {BaseUrl}",
                _service.SanitizedBaseUrl);
            return false;
        }

        if (_guideProcessor.CustomFormatsWithoutScore.Count > 0)
        {
            _log.Information(
                "The below custom formats have no score in the guide or in your YAML config. They will " +
                "still be synced, but no score will be set in your chosen quality profiles");
            foreach (var tuple in _guideProcessor.CustomFormatsWithoutScore)
            {
                _log.Information("{CfList}", tuple);
            }

            _console.Output.WriteLine("");
        }

        if (_guideProcessor.DuplicateScores.Any())
        {
            foreach (var (profileName, duplicates) in _guideProcessor.DuplicateScores)
            foreach (var (trashId, dupeScores) in duplicates)
            {
                _log.Warning(
                    "Custom format with trash ID {TrashId} is duplicated {Count} times in quality profile " +
                    "{ProfileName} with the following scores: {Scores}",
                    trashId, dupeScores.Count, profileName, dupeScores);
            }

            _log.Warning(
                "When the same CF is specified multiple times with different scores in the same quality profile, " +
                "only the score from the first occurrence is used. To resolve the duplication warnings above, " +
                "remove the duplicate trash IDs from your YAML config");

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
                    _log.Warning("Quality Profile refers to CF not found in guide: {TrashId}",
                        customFormat.TrashId);
                    continue;
                }

                _console.Output.WriteLine(profileFormat, "", matchingCf.Name, score);
            }
        }

        _console.Output.WriteLine("");
    }
}
