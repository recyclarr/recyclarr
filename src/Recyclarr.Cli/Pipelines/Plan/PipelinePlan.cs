namespace Recyclarr.Cli.Pipelines.Plan;

internal class PipelinePlan
{
    private readonly Dictionary<string, PlannedCustomFormat> _customFormats = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly Dictionary<string, PlannedQualityProfile> _qualityProfiles = new(
        StringComparer.OrdinalIgnoreCase
    );

    private PlannedQualitySizes? _qualitySizes;
    public bool QualitySizesAvailable => _qualitySizes is not null;
    public PlannedQualitySizes QualitySizes
    {
        get =>
            _qualitySizes
            ?? throw new InvalidOperationException(
                "QualitySizes accessed but quality_definition not configured"
            );
        set => _qualitySizes = value;
    }

    private PlannedMediaNaming? _mediaNaming;
    public bool MediaNamingAvailable => _mediaNaming is not null;
    public PlannedMediaNaming MediaNaming
    {
        get =>
            _mediaNaming
            ?? throw new InvalidOperationException(
                "MediaNaming accessed but media_naming not configured"
            );
        set => _mediaNaming = value;
    }

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
