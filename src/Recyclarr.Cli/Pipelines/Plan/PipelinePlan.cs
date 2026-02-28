using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class PipelinePlan(IDiagnosticPublisher publisher) : IDiagnosticPublisher
{
    public bool HasErrors { get; private set; }

    public void AddError(string message)
    {
        HasErrors = true;
        publisher.AddError(message);
    }

    public void AddWarning(string message)
    {
        publisher.AddWarning(message);
    }

    public void AddDeprecation(string message)
    {
        publisher.AddDeprecation(message);
    }

    private readonly Dictionary<string, PlannedCustomFormat> _customFormats = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly Dictionary<string, PlannedQualityProfile> _qualityProfiles = new(
        StringComparer.OrdinalIgnoreCase
    );

    public PlannedQualitySizes? QualitySizes { get; set; }
    public PlannedSonarrMediaNaming? SonarrMediaNaming { get; set; }
    public PlannedRadarrMediaNaming? RadarrMediaNaming { get; set; }
    public PlannedMediaManagement? MediaManagement { get; set; }

    public IReadOnlyCollection<PlannedCustomFormat> CustomFormats => _customFormats.Values;
    public IReadOnlyCollection<PlannedQualityProfile> QualityProfiles => _qualityProfiles.Values;

    public void AddCustomFormat(PlannedCustomFormat cf)
    {
        _customFormats[cf.Resource.TrashId] = cf;
    }

    public void AddQualityProfile(PlannedQualityProfile profile)
    {
        _qualityProfiles[profile.Name] = profile;
    }

    public PlannedCustomFormat? GetCustomFormat(string trashId)
    {
        return _customFormats.GetValueOrDefault(trashId);
    }

    public PlannedQualityProfile? GetQualityProfile(string name)
    {
        return _qualityProfiles.GetValueOrDefault(name);
    }
}
