using System;
using TrashLib.Radarr.CustomFormat.Cache;

namespace TrashLib.Radarr.CustomFormat.Models.Cache
{
    public class TrashIdMapping : ServiceCacheObject, IEquatable<TrashIdMapping>
    {
        public string TrashId { get; init; } = default!;
        public int CustomFormatId { get; init; }

        public bool Equals(TrashIdMapping? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                TrashId == other.TrashId &&
                CustomFormatId == other.CustomFormatId;
        }

        public override bool Equals(object? obj) =>
            obj is TrashIdMapping other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(TrashId, CustomFormatId);
    }
}
