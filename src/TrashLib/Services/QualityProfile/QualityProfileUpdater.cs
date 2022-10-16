using CliFx.Infrastructure;
using Common.Extensions;
using Serilog;
using TrashLib.Config.Services;
using TrashLib.Services.Common;
using TrashLib.Services.QualityProfile.Api;
using TrashLib.Services.QualityProfile.Processors;

namespace TrashLib.Services.QualityProfile;

internal class QualityProfileUpdater : IQualityProfileUpdater
{
    private readonly IConsole _console;
    private readonly IServerInfo _serverInfo;
    private readonly IQualityProfileProcessor _qualityProfileProcessor;
    private readonly IQualityProfileService _qualityProfileService;

    public QualityProfileUpdater(
        ILogger log,
        IConsole console,
        IServerInfo serverInfo,
        IQualityProfileProcessor qualityProfileProcessor,
        IQualityProfileService qualityProfileService)
    {
        Log = log;

        _console = console;
        _serverInfo = serverInfo;
        _qualityProfileProcessor = qualityProfileProcessor;
        _qualityProfileService = qualityProfileService;
    }

    private ILogger Log { get; }

    public async Task Process(bool isPreview, IEnumerable<QualityProfileConfig> configs, IEnumerable<QualityGroupConfig> groups)
    {

        await _qualityProfileProcessor.BuildQualityProfileDataAsync(groups, configs, _qualityProfileService);

        if (isPreview)
        {
            //TODO: Show what changes will happen to the QualityProfiles
            PreviewQualityProfiles();
        }
        else
        {

        //     // Step 1: Get the Quality Profiles from the Server, get it's ID.
        //     // Step 1a: If the Quality Profile doesn't exist, then create it.
        //     // Step 2: Get the Quality Definitions.
        //     // Step 3: Create the QualityDefinitionConfig for the QualityProfile.
        //     // Step 4: Commit it.

        //     await _persistenceProcessor.PersistCustomFormats(_guideProcessor.ProcessedCustomFormats,
        //         _guideProcessor.DeletedCustomFormatsInCache, _guideProcessor.ProfileScores);

        //     PrintApiStatistics(_persistenceProcessor.Transactions);
                PrintQualityProfileUpdates();

        //     // Cache all the custom formats (using ID from API response).
        //     _cache.Update(_guideProcessor.ProcessedCustomFormats);
        //     _cache.Save();
        // }

        // _persistenceProcessor.Reset();
        // _guideProcessor.Reset();
        }
    }

    private void PrintQualityProfileUpdates()
    {
        // if (_persistenceProcessor.UpdatedScores.Count > 0)
        // {
        //     foreach (var (profileName, scores) in _persistenceProcessor.UpdatedScores)
        //     {
        //         Log.Debug("> Scores updated for quality profile: {ProfileName}", profileName);

        //         foreach (var (customFormatName, score, reason) in scores)
        //         {
        //             Log.Debug("  - {Format}: {Score} ({Reason})", customFormatName, score, reason);
        //         }
        //     }

        //     Log.Information("Updated {ProfileCount} profiles and a total of {ScoreCount} scores",
        //         _persistenceProcessor.UpdatedScores.Keys.Count,
        //         _persistenceProcessor.UpdatedScores.Sum(s => s.Value.Count));
        // }
        // else
        // {
        //     Log.Information("All quality profile scores are already up to date!");
        // }

        // if (_persistenceProcessor.InvalidProfileNames.Count > 0)
        // {
        //     Log.Warning("The following quality profile names are not valid and should either be " +
        //                 "removed or renamed in your YAML config");
        //     Log.Warning("{QualityProfileNames}", _persistenceProcessor.InvalidProfileNames);
        // }
    }
    private void PreviewQualityProfiles()
    {

        _console.Output.WriteLine("");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("      >>> Quality Profiles <<<       ");
        _console.Output.WriteLine("=========================================================");
        _console.Output.WriteLine("");

        // const string profileFormat = "{0,-18} {1,-20} {2,-8}";
        // _console.Output.WriteLine(profileFormat, "Profile", "Custom Format", "Score");
        // _console.Output.WriteLine(string.Concat(Enumerable.Repeat('-', 2 + 18 + 20 + 8)));

        // foreach (var (profileName, scoreMap) in _guideProcessor.ProfileScores)
        // {
        //     _console.Output.WriteLine(profileFormat, profileName, "", "");

        //     foreach (var (customFormat, score) in scoreMap.Mapping)
        //     {
        //         var matchingCf = _guideProcessor.ProcessedCustomFormats
        //             .FirstOrDefault(cf => cf.TrashId.EqualsIgnoreCase(customFormat.TrashId));

        //         if (matchingCf == null)
        //         {
        //             Log.Warning("Quality Profile refers to CF not found in guide: {TrashId}",
        //                 customFormat.TrashId);
        //             continue;
        //         }

        //         _console.Output.WriteLine(profileFormat, "", matchingCf.Name, score);
        //     }
        // }

        _console.Output.WriteLine("");
    }
}
