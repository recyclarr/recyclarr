using TrashLib.Config.Services;
using TrashLib.Services.Common;
using TrashLib.Services.QualityProfile.Models;
using TrashLib.Services.QualityProfile.Api;
using Newtonsoft.Json.Linq;

namespace TrashLib.Services.QualityProfile.Processors;

public interface IQualityProfileProcessorSteps
{
   // IQualityGroupStep QualityGroup { get; }
}

internal class QualityProfileProcessor : IQualityProfileProcessor
{
    private readonly Func<IQualityProfileProcessorSteps> _stepsFactory;
    private IList<JObject>? _qualityProfileJson;
    private IQualityProfileProcessorSteps _steps;

    public QualityProfileProcessor(Func<IQualityProfileProcessorSteps> stepsFactory)
    {
        _stepsFactory = stepsFactory;
        _steps = stepsFactory();
    }

    public async Task BuildQualityProfileDataAsync(IEnumerable<QualityGroupConfig> qualityGroupConfig, IEnumerable<QualityProfileConfig> qualityProfileConfig, IQualityProfileService qualityProfileService)
    {
        //_qualityProfileJson ??= await qualityProfileService.GetQualityProfiles();

        var serviceProfiles = await qualityProfileService.GetQualityProfiles();


        // Get the Quality Profiles from Radarr. And join them based on the profiles we are looking for.
        // If the profile doesn't yet exist than the extra fields wont' exist.
        var currentProfiles = qualityProfileConfig.GroupJoin(serviceProfiles,
            s => s.Name,
            p => p.Value<string>("name"),
            (s, p) =>  new {
                    s.Name,
                    p
            },
            StringComparer.InvariantCultureIgnoreCase);

        Console.WriteLine("Current Profiles Length: " + currentProfiles.Count());

        foreach (var configProfile in currentProfiles) {
            // It doesn't exist
            if (configProfile.p.Count() == 0) {
                Console.WriteLine("Would create profile: " + configProfile.Name);
            } else {
                Console.WriteLine("Profile exists: " + configProfile.Name);
            }
        }



        // // Step 2: Use the processed custom formats from step 1 to process the configuration.
        // // CFs in config not in the guide are filtered out.
        // // Actual CF objects are associated to the quality profile objects to reduce lookups
        // _steps.Config.Process(_steps.CustomFormat.ProcessedCustomFormats, listOfConfigs);

        // // Step 3: Use the processed config (which contains processed CFs) to process the quality profile scores.
        // // Score precedence logic is utilized here to decide the CF score per profile (same CF can actually have
        // // different scores depending on which profile it goes into).
        // _steps.QualityProfile.Process(_steps.Config.ConfigData);

        //return Task.CompletedTask;
    }

    public void Reset()
    {
       // _steps = _stepsFactory();
    }
}
