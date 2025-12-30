namespace Recyclarr.Cli.Processors.CacheRebuild;

internal enum CacheRebuildState
{
    // Cache changes (actions taken)
    Adopted,
    Corrected,
    Removed,

    // Informational (no cache change)
    Skipped,
    NotInService,
    Unchanged,
    Preserved,
    Ambiguous,
}
