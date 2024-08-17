using Recyclarr.Compatibility.Sonarr;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class SonarrQualityItemLimitFetcher(ISonarrCapabilityFetcher capabilityFetcher) : IQualityItemLimitFetcher
{
    private QualityItemLimits? _cachedLimits;

    public async Task<QualityItemLimits> GetLimits(CancellationToken ct)
    {
        // ReSharper disable once InvertIf
        if (_cachedLimits is null)
        {
            var capabilities = await capabilityFetcher.GetCapabilities(ct);
            _cachedLimits = capabilities switch
            {
                {QualityDefinitionLimitsIncreased: true} => new QualityItemLimits(1000m, 995m),
                _ => new QualityItemLimits(400m, 395m)
            };
        }

        return _cachedLimits;
    }
}
