using Recyclarr.Compatibility.Radarr;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class RadarrQualityItemLimitFetcher(IRadarrCapabilityFetcher capabilityFetcher) : IQualityItemLimitFetcher
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
                {QualityDefinitionLimitsIncreased: true} => new QualityItemLimits(2000m, 1999m),
                _ => new QualityItemLimits(400m, 399m)
            };
        }

        return _cachedLimits;
    }
}
