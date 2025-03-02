using System.Collections.ObjectModel;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal record CustomFormatTransactionData
{
    public Collection<TrashIdMapping> DeletedCustomFormats { get; } = [];
    public Collection<CustomFormatData> NewCustomFormats { get; } = [];
    public Collection<CustomFormatData> UpdatedCustomFormats { get; } = [];
    public Collection<ConflictingCustomFormat> ConflictingCustomFormats { get; } = [];
    public Collection<CustomFormatData> UnchangedCustomFormats { get; } = [];

    public int TotalCustomFormatChanges =>
        NewCustomFormats.Count + UpdatedCustomFormats.Count + DeletedCustomFormats.Count;
}
