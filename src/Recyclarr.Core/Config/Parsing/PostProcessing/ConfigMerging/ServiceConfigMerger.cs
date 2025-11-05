using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

[SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
public abstract class ServiceConfigMerger<T>
    where T : ServiceConfigYaml
{
    protected static TVal? Combine<TVal>(TVal? a, TVal? b, Func<TVal, TVal, TVal?> combine)
    {
        if (b is null)
        {
            return a;
        }

        if (a is null)
        {
            return b;
        }

        return combine(a, b);
    }

    public virtual T Merge(T a, T b)
    {
        return a with
        {
            CustomFormats = Combine(a.CustomFormats, b.CustomFormats, MergeCustomFormats),
            QualityProfiles = MergeQualityProfiles(a.QualityProfiles, b.QualityProfiles),
            QualityDefinition = Combine(
                a.QualityDefinition,
                b.QualityDefinition,
                (a1, b1) =>
                    a1 with
                    {
                        Type = b1.Type ?? a1.Type,
                        PreferredRatio = b1.PreferredRatio ?? a1.PreferredRatio,
                        Qualities = MergeQualitySizeItems(a1.Qualities, b1.Qualities),
                    }
            ),

            DeleteOldCustomFormats = b.DeleteOldCustomFormats ?? a.DeleteOldCustomFormats,

            ReplaceExistingCustomFormats =
                b.ReplaceExistingCustomFormats ?? a.ReplaceExistingCustomFormats,
        };
    }

    private sealed record FlattenedCfs(
        string? ProfileName,
        int? Score,
        IReadOnlyCollection<string> TrashIds
    );

    private static IReadOnlyCollection<CustomFormatConfigYaml> MergeCustomFormats(
        IReadOnlyCollection<CustomFormatConfigYaml> a,
        IReadOnlyCollection<CustomFormatConfigYaml> b
    )
    {
        var flattenedA = FlattenCfs(a);
        var flattenedB = FlattenCfs(b);

        return flattenedA
            // This builds a list of TrashIds in side B that are assigned to matching profiles in A
            .Select(x =>
                (
                    A: x,
                    B: flattenedB
                        .Where(y => y.ProfileName.EqualsIgnoreCase(x.ProfileName)) // Ignore score
                        .SelectMany(y => y.TrashIds)
                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                        .ToList()
                )
            )
            // Add everything on side A that isn't on side B
            .Select(x => new CustomFormatConfigYaml
            {
                TrashIds = x
                    .A.TrashIds.Except(x.B, StringComparer.InvariantCultureIgnoreCase)
                    .ToList(),
                AssignScoresTo = x.A.ProfileName is not null
                    ? new[]
                    {
                        new QualityScoreConfigYaml { Name = x.A.ProfileName, Score = x.A.Score },
                    }
                    : null,
            })
            .Concat(b)
            .ToList();

        static List<FlattenedCfs> FlattenCfs(IEnumerable<CustomFormatConfigYaml> cfs)
        {
            return cfs.Where(x => x.TrashIds is not null)
                .SelectMany(x =>
                    x is { AssignScoresTo.Count: > 0 }
                        ? x.AssignScoresTo.Select(y => new FlattenedCfs(
                            y.Name,
                            y.Score,
                            x.TrashIds!
                        ))
                        : [new FlattenedCfs(null, null, x.TrashIds!)]
                )
                .GroupBy(x => (Name: x.ProfileName, x.Score))
                .Select(x => new FlattenedCfs(
                    x.Key.Name,
                    x.Key.Score,
                    x.SelectMany(y => y.TrashIds).ToList()
                ))
                .ToList();
        }
    }

    private static IReadOnlyCollection<QualityProfileConfigYaml>? MergeQualityProfiles(
        IReadOnlyCollection<QualityProfileConfigYaml>? a,
        IReadOnlyCollection<QualityProfileConfigYaml>? b
    )
    {
        return Combine(
            a,
            b,
            (a1, b1) =>
            {
                return a1.FullOuterHashJoin(
                        b1,
                        x => x.Name,
                        x => x.Name,
                        l => l,
                        r => r,
                        MergeQualityProfile,
                        StringComparer.InvariantCultureIgnoreCase
                    )
                    .ToList();
            }
        );
    }

    private static QualityProfileConfigYaml MergeQualityProfile(
        QualityProfileConfigYaml a,
        QualityProfileConfigYaml b
    )
    {
        return a with
        {
            Upgrade = Combine(
                a.Upgrade,
                b.Upgrade,
                (a1, b1) =>
                    a1 with
                    {
                        Allowed = b1.Allowed ?? a1.Allowed,
                        UntilQuality = b1.UntilQuality ?? a1.UntilQuality,
                        UntilScore = b1.UntilScore ?? a1.UntilScore,
                    }
            ),
            MinFormatScore = b.MinFormatScore ?? a.MinFormatScore,
            MinUpgradeFormatScore = b.MinUpgradeFormatScore ?? a.MinUpgradeFormatScore,
            QualitySort = b.QualitySort ?? a.QualitySort,
            ScoreSet = b.ScoreSet ?? a.ScoreSet,
            ResetUnmatchedScores = Combine(
                a.ResetUnmatchedScores,
                b.ResetUnmatchedScores,
                (a1, b1) =>
                    a1 with
                    {
                        Enabled = b1.Enabled ?? a1.Enabled,
                        Except = Combine(
                            a1.Except,
                            b1.Except,
                            (a2, b2) =>
                                Combine(
                                    a2,
                                    b2,
                                    (a3, b3) =>
                                        a3.Concat(b3)
                                            .Distinct(StringComparer.InvariantCultureIgnoreCase)
                                            .ToList()
                                )
                        ),
                    }
            ),
            Qualities = b.Qualities ?? a.Qualities,
        };
    }

    private static IReadOnlyCollection<QualitySizeItemConfigYaml>? MergeQualitySizeItems(
        IReadOnlyCollection<QualitySizeItemConfigYaml>? a,
        IReadOnlyCollection<QualitySizeItemConfigYaml>? b
    )
    {
        return Combine(
            a,
            b,
            (a1, b1) =>
            {
                return a1.FullOuterHashJoin(
                        b1,
                        x => x.Name,
                        x => x.Name,
                        l => l,
                        r => r,
                        MergeQualitySizeItem,
                        StringComparer.InvariantCultureIgnoreCase
                    )
                    .ToList();
            }
        );
    }

    private static QualitySizeItemConfigYaml MergeQualitySizeItem(
        QualitySizeItemConfigYaml a,
        QualitySizeItemConfigYaml b
    )
    {
        return a with
        {
            Min = b.Min ?? a.Min,
            Max = b.Max ?? a.Max,
            Preferred = b.Preferred ?? a.Preferred,
        };
    }
}
