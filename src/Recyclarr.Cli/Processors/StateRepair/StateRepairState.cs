namespace Recyclarr.Cli.Processors.StateRepair;

internal enum StateRepairState
{
    // State changes (actions taken)
    Adopted,
    Corrected,
    Removed,

    // Informational (no state change)
    Skipped,
    NotInService,
    Unchanged,
    Preserved,
    Ambiguous,
}
