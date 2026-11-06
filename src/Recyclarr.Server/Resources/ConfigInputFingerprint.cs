using System.Collections.Immutable;

namespace Recyclarr.Server.Resources;

/// <summary>
/// The contents of the configuration inputs (ADR-019) at one point in time: a content hash per
/// path, or null when the file is missing. Equal fingerprints mean no input changed.
/// </summary>
internal sealed class ConfigInputFingerprint(ImmutableSortedDictionary<string, string?> hashes)
    : IEquatable<ConfigInputFingerprint>
{
    public IReadOnlyDictionary<string, string?> Hashes => hashes;

    public bool Equals(ConfigInputFingerprint? other) =>
        other is not null && hashes.SequenceEqual(other.Hashes);

    public override bool Equals(object? obj) => Equals(obj as ConfigInputFingerprint);

    public override int GetHashCode() => hashes.Count;
}
