namespace Recyclarr.Cli.Processors.CacheRebuild;

internal record CacheRebuildStats(
    // Cache changes
    int Adopted,
    int Corrected,
    int Removed,
    // Informational
    int Skipped,
    int NotInService,
    int Unchanged,
    int Preserved
)
{
    public bool HasChanges => Adopted > 0 || Corrected > 0 || Removed > 0;

    public int TotalEntries => Adopted + Corrected + Unchanged + Preserved;
}
