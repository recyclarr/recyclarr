using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.Reusable;

internal static class NewSyncState
{
    public static TrashIdMapping Mapping(string trashId, string name, int serviceId)
    {
        return new TrashIdMapping
        {
            TrashId = trashId,
            Name = name,
            ServiceId = serviceId,
        };
    }
}
