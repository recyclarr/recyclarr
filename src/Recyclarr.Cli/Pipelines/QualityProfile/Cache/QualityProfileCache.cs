using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Cache;

internal class QualityProfileCache(QualityProfileCacheObject cacheObject)
    : TrashIdCache<QualityProfileCacheObject>(cacheObject)
{
    public int? FindId(QualityProfileResource qp)
    {
        return FindId(qp.TrashId);
    }

    public int? FindIdByTrashId(string? trashId)
    {
        return trashId is null ? null : FindId(trashId);
    }

    public void Update(
        QualityProfileTransactionData transactions,
        IEnumerable<QualityProfileDto> serviceProfiles
    )
    {
        // Only cache guide-backed profiles (those with TrashId)
        var syncedMappings = transactions
            .ChangedProfiles.Concat(transactions.UnchangedProfiles)
            .Where(p => p.Profile.TrashId is not null && p.Profile.ProfileDto.Id is not null)
            .Select(p => new TrashIdMapping(
                p.Profile.TrashId!,
                p.Profile.ProfileName,
                p.Profile.ProfileDto.Id!.Value
            ));

        // No deleted profiles for QPs (no delete toggle - high stakes)
        var deletedIds = Enumerable.Empty<int>();
        var validServiceIds = serviceProfiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value);

        Update(syncedMappings, deletedIds, validServiceIds);
    }
}
