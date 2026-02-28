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

    private PlannedSonarrMediaNaming? _sonarrMediaNaming;
    public bool SonarrMediaNamingAvailable => _sonarrMediaNaming is not null;
    public PlannedSonarrMediaNaming SonarrMediaNaming
    {
        get =>
            _sonarrMediaNaming
            ?? throw new InvalidOperationException(
                "SonarrMediaNaming accessed but media_naming not configured for Sonarr"
            );
        set => _sonarrMediaNaming = value;
    }

    private PlannedRadarrMediaNaming? _radarrMediaNaming;
    public bool RadarrMediaNamingAvailable => _radarrMediaNaming is not null;
    public PlannedRadarrMediaNaming RadarrMediaNaming
    {
        get =>
            _radarrMediaNaming
            ?? throw new InvalidOperationException(
                "RadarrMediaNaming accessed but media_naming not configured for Radarr"
            );
        set => _radarrMediaNaming = value;
    }

    private PlannedMediaManagement? _mediaManagement;
    public bool MediaManagementAvailable => _mediaManagement is not null;
    public PlannedMediaManagement MediaManagement
    {
        get =>
            _mediaManagement
            ?? throw new InvalidOperationException(
                "MediaManagement accessed but media_management not configured"
            );
        set => _mediaManagement = value;
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
