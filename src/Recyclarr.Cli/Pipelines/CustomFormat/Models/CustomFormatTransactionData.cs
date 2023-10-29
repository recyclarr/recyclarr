using System.Collections.ObjectModel;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

public record CustomFormatTransactionData
{
    public Collection<TrashIdMapping> DeletedCustomFormats { get; } = new();
    public Collection<CustomFormatData> NewCustomFormats { get; } = new();
    public Collection<CustomFormatData> UpdatedCustomFormats { get; } = new();
    public Collection<ConflictingCustomFormat> ConflictingCustomFormats { get; } = new();
    public Collection<CustomFormatData> UnchangedCustomFormats { get; } = new();
}
