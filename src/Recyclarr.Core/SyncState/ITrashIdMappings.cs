using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.SyncState;

/// <summary>
/// Contract for state objects that store trash_id to service ID mappings.
/// Implemented by CustomFormatMappings, QualityProfileMappings, etc.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1002:Do not expose generic lists",
    Justification = "POCO needing List methods"
)]
public interface ITrashIdMappings
{
    List<TrashIdMapping> Mappings { get; }
}
