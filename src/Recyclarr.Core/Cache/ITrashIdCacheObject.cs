using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Cache;

/// <summary>
/// Contract for cache objects that store trash_id to service ID mappings.
/// Implemented by CustomFormatCacheObject, QualityProfileCacheObject, etc.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1002:Do not expose generic lists",
    Justification = "POCO needing List methods"
)]
public interface ITrashIdCacheObject
{
    List<TrashIdMapping> Mappings { get; }
}
