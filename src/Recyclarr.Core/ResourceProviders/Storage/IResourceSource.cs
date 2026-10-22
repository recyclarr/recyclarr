using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public enum SourceUpdateOutcome
{
    Updated,
    Unchanged,

    // The remote could not be reached; the previous data stays in place.
    FailedUsingCache,
}

/// <summary>
/// Where a remote source records its refresh state, and the identity of what it fetches. An
/// identity change (for example, a new clone URL or reference) makes the cached data stale.
/// </summary>
public sealed record SourceTracking(IFileInfo StateFile, string Identity);

/// <summary>
/// One configured resource provider's data on disk, and how to change it. A source is changed
/// only by its single writer (the resource coordinator), never while readers use its directories.
/// </summary>
/// <remarks>
/// A refresh updates the source, validates configuration against the new data, then either marks
/// it good or reverts it. Sources without a remote origin (local directories) never change, so
/// their update, mark, and revert do nothing.
/// </remarks>
public interface IResourceSource
{
    ResourceProvider Config { get; }
    IProviderTypeStrategy Strategy { get; }

    /// <summary>Null for sources without a remote origin; they are never refreshed.</summary>
    SourceTracking? Tracking { get; }

    /// <summary>The directories that currently hold this source's data; empty when none exist.</summary>
    IReadOnlyCollection<IDirectoryInfo> Roots { get; }

    /// <summary>
    /// Brings the data up to date with its origin. Throws when the origin cannot be reached and no
    /// earlier data exists.
    /// </summary>
    Task<SourceUpdateOutcome> UpdateAsync(CancellationToken ct);

    /// <summary>Records the current data as the version to restore if a later update is bad.</summary>
    Task MarkGoodAsync(CancellationToken ct);

    /// <summary>
    /// Restores the data last marked good. Returns false when nothing is marked, for example after
    /// a repair deleted and re-cloned the data.
    /// </summary>
    Task<bool> RevertToGoodAsync(CancellationToken ct);
}
