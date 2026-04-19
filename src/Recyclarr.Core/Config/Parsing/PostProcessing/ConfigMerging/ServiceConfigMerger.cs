namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

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
            CustomFormatGroups = Combine(
                a.CustomFormatGroups,
                b.CustomFormatGroups,
                MergeCustomFormatGroups
            ),
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
            MediaManagement = Combine(
                a.MediaManagement,
                b.MediaManagement,
                (a1, b1) =>
                    a1 with
                    {
                        PropersAndRepacks = b1.PropersAndRepacks ?? a1.PropersAndRepacks,
                    }
            ),
        };
    }

    private static IReadOnlyCollection<CustomFormatConfigYaml> MergeCustomFormats(
        IReadOnlyCollection<CustomFormatConfigYaml> a,
        IReadOnlyCollection<CustomFormatConfigYaml> b
    )
    {
        // Two-pass merge: first normalize entry-level scores to per-profile scores on both sides,
        // then subtract B's trash IDs from A's entries for matching profile keys.
        var normalizedA = NormalizeEntryScores(a);
        var normalizedB = NormalizeEntryScores(b);

        // Build lookup of trash IDs per profile key from side B
        var trashIdsByProfileB = normalizedB
            .Where(x => x.AssignScoresTo is not null)
            .SelectMany(x =>
                x.AssignScoresTo!.Select(profile => // non-null: filtered by Where above
                    (ProfileKey: GetProfileKey(profile), x.TrashIds)
                )
            )
            .Where(x => x.ProfileKey is not null)
            .GroupBy(x => x.ProfileKey!, StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.SelectMany(x => x.TrashIds ?? [])
                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                        .ToList(),
                StringComparer.InvariantCultureIgnoreCase
            );

        // For each normalized A entry, subtract matching B trash IDs
        var mergedA = normalizedA.Select(x =>
        {
            // NormalizeEntryScores guarantees exactly one profile per entry with AssignScoresTo
            var profile = x.AssignScoresTo?.SingleOrDefault();
            if (profile is null)
            {
                return x;
            }

            var profileKey = GetProfileKey(profile);

            if (
                profileKey is null
                || !trashIdsByProfileB.TryGetValue(profileKey, out var bTrashIds)
            )
            {
                return x;
            }

            var remainingTrashIds = x
                .TrashIds?.Except(bTrashIds, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            return x with
            {
                TrashIds = remainingTrashIds,
            };
        });

        return [.. mergedA, .. normalizedB];

        static string? GetProfileKey(QualityScoreConfigYaml? profile) =>
            profile?.TrashId ?? profile?.Name;

        static List<CustomFormatConfigYaml> NormalizeEntryScores(
            IEnumerable<CustomFormatConfigYaml> cfs
        )
        {
            return cfs.Where(x => x.TrashIds is not null)
                .SelectMany(x =>
                    x is { AssignScoresTo.Count: > 0 }
                        ? x.AssignScoresTo.Select(profile => new CustomFormatConfigYaml
                        {
                            TrashIds = x.TrashIds,
                            AssignScoresTo = [profile with { Score = profile.Score ?? x.Score }],
                        })
                        : [x]
                )
                // Consolidate trash IDs when multiple entries target the same profile-key/score
                .GroupBy(x =>
                {
                    var profile = x.AssignScoresTo?.SingleOrDefault();
                    return (
                        profileKey: GetProfileKey(profile),
                        trashId: profile?.TrashId,
                        name: profile?.Name,
                        score: profile?.Score
                    );
                })
                .Select(g => new CustomFormatConfigYaml
                {
                    TrashIds = g.SelectMany(x => x.TrashIds ?? [])
                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                        .ToList(),
                    AssignScoresTo = g.Key.profileKey is not null
                        ?
                        [
                            new QualityScoreConfigYaml
                            {
                                TrashId = g.Key.trashId,
                                Name = g.Key.name,
                                Score = g.Key.score,
                            },
                        ]
                        // No profile key: preserve original value (null stays null, [] stays [])
                        : g.First().AssignScoresTo,
                })
                .ToList();
        }
    }

    private static CustomFormatGroupsConfigYaml MergeCustomFormatGroups(
        CustomFormatGroupsConfigYaml a,
        CustomFormatGroupsConfigYaml b
    )
    {
        return new CustomFormatGroupsConfigYaml
        {
            Skip = MergeSkipList(a.Skip, b.Skip),
            Add = MergeAddList(a.Add, b.Add),
        };

        static IReadOnlyCollection<string>? MergeSkipList(
            IReadOnlyCollection<string>? a,
            IReadOnlyCollection<string>? b
        )
        {
            return Combine(
                a,
                b,
                (a1, b1) =>
                    a1.Concat(b1).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList()
            );
        }

        static IReadOnlyCollection<CustomFormatGroupConfigYaml>? MergeAddList(
            IReadOnlyCollection<CustomFormatGroupConfigYaml>? a,
            IReadOnlyCollection<CustomFormatGroupConfigYaml>? b
        )
        {
            return Combine(
                a,
                b,
                (a1, b1) =>
                    a1.FullOuterHashJoin(
                            b1,
                            x => x.TrashId,
                            x => x.TrashId,
                            l => l,
                            r => r,
                            (l, r) =>
                                l with
                                {
                                    AssignScoresTo = r.AssignScoresTo ?? l.AssignScoresTo,
                                    Select = r.Select ?? l.Select,
                                },
                            StringComparer.InvariantCultureIgnoreCase
                        )
                        .ToList()
            );
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
                // Composite key: profiles merge only when both trash_id and name match.
                // This allows multiple profiles sharing a trash_id (with different names)
                // to pass through includes without collapsing.
                return a1.FullOuterHashJoin(
                        b1,
                        x => $"{x.TrashId}\0{x.Name}",
                        x => $"{x.TrashId}\0{x.Name}",
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
                        ExceptPatterns = Combine(
                            a1.ExceptPatterns,
                            b1.ExceptPatterns,
                            (a2, b2) =>
                                Combine(
                                    a2,
                                    b2,
                                    (a3, b3) =>
                                        a3.Concat(b3).Distinct(StringComparer.Ordinal).ToList()
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
