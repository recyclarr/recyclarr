namespace Recyclarr.Sync.Results;

internal static class SyncResultStatusAggregation
{
    public static SyncResultStatus From(
        IEnumerable<SyncResultStatus> statuses,
        bool hasBoundaryFailure
    )
    {
        var hasCompletedWork = false;
        var hasIncompleteWork = hasBoundaryFailure;

        foreach (var status in statuses)
        {
            hasCompletedWork |= status is SyncResultStatus.Succeeded or SyncResultStatus.Partial;
            hasIncompleteWork |= status is not SyncResultStatus.Succeeded;
        }

        if (!hasIncompleteWork)
        {
            return SyncResultStatus.Succeeded;
        }

        return hasCompletedWork ? SyncResultStatus.Partial : SyncResultStatus.Failed;
    }
}
