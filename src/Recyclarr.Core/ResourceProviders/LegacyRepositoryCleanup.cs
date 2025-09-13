using System.IO.Abstractions;
using Recyclarr.Common.Extensions;

namespace Recyclarr.ResourceProviders;

/// <summary>
/// Utility for cleaning up legacy Git repositories that conflict with new hierarchical structure.
/// </summary>
internal static class LegacyRepositoryCleanup
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(LegacyRepositoryCleanup));

    /// <summary>
    /// Cleans up a legacy Git repository if it exists at the specified parent path.
    /// This prevents conflicts between old single repositories and new hierarchical sub-repositories.
    /// </summary>
    /// <param name="parentPath">The parent directory that may contain a legacy Git repository</param>
    public static void CleanLegacyRepository(IDirectoryInfo parentPath)
    {
        if (!parentPath.Exists)
        {
            return;
        }

        var gitDir = parentPath.SubDirectory(".git");
        if (!gitDir.Exists)
        {
            return;
        }

        Log.Information(
            "Cleaning up legacy repository at {Path} to enable hierarchical structure",
            parentPath.FullName
        );

        parentPath.RecursivelyDeleteReadOnly();
    }
}
