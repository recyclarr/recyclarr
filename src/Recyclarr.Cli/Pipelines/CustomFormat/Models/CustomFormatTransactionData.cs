using System.Collections.ObjectModel;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal record CustomFormatTransactionData
{
    public Collection<TrashIdMapping> DeletedCustomFormats { get; } = [];
    public Collection<CustomFormatResource> NewCustomFormats { get; } = [];
    public Collection<CustomFormatResource> UpdatedCustomFormats { get; } = [];
    public Collection<ConflictingCustomFormat> ConflictingCustomFormats { get; } = [];
    public Collection<AmbiguousMatch> AmbiguousCustomFormats { get; } = [];
    public Collection<CustomFormatResource> UnchangedCustomFormats { get; } = [];
    public Collection<TrashIdMapping> InvalidCacheEntries { get; } = [];

    public int TotalCustomFormatChanges =>
        NewCustomFormats.Count + UpdatedCustomFormats.Count + DeletedCustomFormats.Count;
}
