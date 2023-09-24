using JetBrains.Annotations;
using Recyclarr.Common;

namespace Recyclarr.ServarrApi.Tag;

public class SonarrTag
{
    public static IEqualityComparer<SonarrTag> Comparer { get; } =
        new GenericEqualityComparer<SonarrTag>((x, y) => x.Id == y.Id, x => x.Id);

    public string Label { get; [UsedImplicitly] set; } = "";
    public int Id { get; [UsedImplicitly] set; }
}
