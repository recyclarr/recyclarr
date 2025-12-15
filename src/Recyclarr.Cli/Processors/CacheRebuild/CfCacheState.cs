namespace Recyclarr.Cli.Processors.CacheRebuild;

internal enum CfCacheState
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
