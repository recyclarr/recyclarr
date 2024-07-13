using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.Cache;

public abstract record CacheObject([property: JsonIgnore] int LatestVersion)
{
    public int Version { get; init; } = LatestVersion;
    public string InstanceName { [UsedImplicitly] get; set; } = "";
}
