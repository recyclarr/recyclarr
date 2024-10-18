namespace Recyclarr.TrashGuide.QualitySize;

public record QualityItemLimits(decimal MaxLimit, decimal PreferredLimit);

public interface IQualityItemLimitFetcher
{
    Task<QualityItemLimits> GetLimits(CancellationToken ct);
}
