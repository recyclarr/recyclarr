using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

[SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
public abstract class ServiceConfigMerger<T> where T : ServiceConfigYaml
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
            CustomFormats = Combine(a.CustomFormats, b.CustomFormats, (x, y) => x.Concat(y).ToList()),
            QualityProfiles = MergeQualityProfiles(a.QualityProfiles, b.QualityProfiles),
            QualityDefinition = Combine(a.QualityDefinition, b.QualityDefinition,
                (x, y) => x with
                {
                    Type = y.Type ?? x.Type,
                    PreferredRatio = y.PreferredRatio ?? x.PreferredRatio
                }),

            DeleteOldCustomFormats =
            b.DeleteOldCustomFormats ?? a.DeleteOldCustomFormats,

            ReplaceExistingCustomFormats =
            b.ReplaceExistingCustomFormats ?? a.ReplaceExistingCustomFormats
        };
    }

    private static IReadOnlyCollection<QualityProfileConfigYaml>? MergeQualityProfiles(
        IReadOnlyCollection<QualityProfileConfigYaml>? a,
        IReadOnlyCollection<QualityProfileConfigYaml>? b)
    {
        return Combine(a, b, (a1, b1) =>
        {
            return a1
                .FullOuterJoin(b1, JoinType.Hash,
                    x => x.Name,
                    x => x.Name,
                    l => l,
                    r => r,
                    MergeQualityProfile,
                    StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        });
    }

    private static QualityProfileConfigYaml MergeQualityProfile(QualityProfileConfigYaml a, QualityProfileConfigYaml b)
    {
        return a with
        {
            Upgrade = Combine(a.Upgrade, b.Upgrade, (a1, b1) => a1 with
            {
                Allowed = b1.Allowed ?? a1.Allowed,
                UntilQuality = b1.UntilQuality ?? a1.UntilQuality,
                UntilScore = b1.UntilScore ?? a1.UntilScore
            }),
            MinFormatScore = b.MinFormatScore ?? a.MinFormatScore,
            QualitySort = b.QualitySort ?? a.QualitySort,
            ScoreSet = b.ScoreSet ?? a.ScoreSet,
            ResetUnmatchedScores = Combine(a.ResetUnmatchedScores, b.ResetUnmatchedScores, (a1, b1) => a1 with
            {
                Enabled = b1.Enabled ?? a1.Enabled,
                Except = Combine(a1.Except, b1.Except, (a2, b2) => Combine(a2, b2, (a3, b3) => a3
                    .Concat(b3)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToList()))
            }),
            Qualities = b.Qualities ?? a.Qualities
        };
    }
}
