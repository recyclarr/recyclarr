using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal record StateRepairResult(
    List<TrashIdMapping> Mappings,
    StateRepairStats Stats,
    List<StateRepairDetail> Details
);
