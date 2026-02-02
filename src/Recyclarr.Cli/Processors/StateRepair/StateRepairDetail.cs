namespace Recyclarr.Cli.Processors.StateRepair;

internal record StateRepairDetail(
    string Name,
    string TrashId,
    string? CachedTrashId,
    int? ServiceId,
    int? CachedServiceId,
    StateRepairState State
);
