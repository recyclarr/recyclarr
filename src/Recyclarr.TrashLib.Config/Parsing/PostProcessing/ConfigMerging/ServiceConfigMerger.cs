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
        return Combine(a, b, (x1, y1) =>
        {
            return x1
                .FullOuterJoin(y1, JoinType.Hash,
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
            Upgrade = Combine(a.Upgrade, b.Upgrade, (x1, y1) => x1 with
            {
                Allowed = y1.Allowed ?? x1.Allowed,
                UntilQuality = y1.UntilQuality ?? x1.UntilQuality,
                UntilScore = y1.UntilScore ?? x1.UntilScore
            }),
            MinFormatScore = b.MinFormatScore ?? a.MinFormatScore,
            QualitySort = b.QualitySort ?? a.QualitySort,
            ScoreSet = b.ScoreSet ?? a.ScoreSet,
            ResetUnmatchedScores = Combine(a.ResetUnmatchedScores, b.ResetUnmatchedScores, (x1, y1) => x1 with
            {
                Enabled = y1.Enabled ?? x1.Enabled,
                Except = Combine(x1.Except, y1.Except, (x2, y2) => Combine(x2, y2, (x3, y3) => x3
                    .Concat(y3)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToList()))
            }),
            Qualities = Combine(a.Qualities, b.Qualities, (x1, y1) => x1.Concat(y1).ToList())
        };
    }
}
