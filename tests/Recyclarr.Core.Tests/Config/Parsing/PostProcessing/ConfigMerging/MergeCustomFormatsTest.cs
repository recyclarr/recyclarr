using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

internal sealed class MergeCustomFormatsTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id1", "id2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "c", Score = 100 }],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id1", "id2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "c", Score = 100 }],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    public void Non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id1", "id2"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "c", Score = 100 },
                        new QualityScoreConfigYaml { Name = "d", Score = 101 },
                        new QualityScoreConfigYaml { Name = "e", Score = 102 },
                    ],
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "f", Score = 100 }],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id3", "id4"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "d", Score = 200 }],
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id5", "id6"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "e", Score = 300 }],
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = ["id1"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "c", Score = 50 }],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "c", Score = 100 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id1", "id2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "d", Score = 101 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id1", "id2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "e", Score = 102 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "f", Score = 100 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id3", "id4"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "d", Score = 200 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id5", "id6"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "e", Score = 300 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["id1"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "c", Score = 50 },
                            ],
                        },
                    ],
                }
            );
    }

    [Test]
    public void Entry_level_score_is_resolved_during_merge()
    {
        // Side A uses an entry-level default score; side B targets the same profile with a
        // matching per-profile score. After flattening, both sides should dedupe to the right
        // side only (since right wins), proving the default was correctly resolved.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    Score = 42,
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "Profile A" }],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A", Score = 42 },
                    ],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        // Left entry has had cf1 removed because right targets the same profile/score
        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "Profile A", Score = 42 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf1"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "Profile A", Score = 42 },
                            ],
                        },
                    ],
                }
            );
    }

    [Test]
    public void Entry_level_score_overridden_by_per_profile_score_during_merge()
    {
        // Entry-level default of 100, but Profile B has an override of 200. After merge the
        // flattened entries should show the effective scores in the per-profile output.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    Score = 100,
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A" },
                        new QualityScoreConfigYaml { Name = "Profile B", Score = 200 },
                    ],
                },
            ],
        };

        // Right side targets Profile A with the same effective score (100) for cf1, so that
        // trash id should be removed from the left entry. Profile B (score 200) should be
        // untouched.
        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A", Score = 100 },
                    ],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml
                        {
                            TrashIds = [],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "Profile A", Score = 100 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf1"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "Profile B", Score = 200 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf1"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "Profile A", Score = 100 },
                            ],
                        },
                    ],
                }
            );
    }

    [Test]
    public void Entry_level_score_on_right_side_survives_merge()
    {
        // Right-side entry-level score must be preserved through the merge, mirroring how the
        // left side handles it. Left targets Profile A with a per-profile score; right uses an
        // entry-level default for Profile B. Nothing dedupes between them.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A", Score = 50 },
                    ],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf2"],
                    Score = 75,
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "Profile B" }],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        // After the runtime conversion, Profile B on the right entry should resolve to score 75
        // from the entry-level default. Structurally, the right entry is concatenated verbatim.
        var radarrConfig = result.ToSonarrConfiguration("test", null);

        radarrConfig
            .CustomFormats.SelectMany(cf => cf.AssignScoresTo)
            .Should()
            .BeEquivalentTo([
                new AssignScoresToConfig { Name = "Profile A", Score = 50 },
                new AssignScoresToConfig { Name = "Profile B", Score = 75 },
            ]);
    }

    [Test]
    public void Multi_profile_right_entry_is_split_into_per_profile_entries()
    {
        // A B-side entry with multiple profiles must be split into separate per-profile output
        // entries after normalization, mirroring how A-side entries are handled.
        // Left must be non-null to invoke MergeCustomFormats; use an unrelated entry.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf0"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "Other", Score = 50 }],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "P1", Score = 100 },
                        new QualityScoreConfigYaml { Name = "P2", Score = 200 },
                    ],
                },
            ],
        };

        var sut = new SonarrConfigMerger();
        var result = sut.Merge(leftConfig, rightConfig);

        result
            .CustomFormats.Should()
            .ContainEquivalentOf(
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "P1", Score = 100 }],
                }
            )
            .And.ContainEquivalentOf(
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "P2", Score = 200 }],
                }
            );
    }

    [Test]
    public void Empty_assign_scores_to_is_preserved_as_empty_not_null()
    {
        // YAML `assign_scores_to: []` deserializes to an empty collection, not null.
        // The merge must not silently coerce an empty list to null; the two values are
        // distinct in the YAML data model even if downstream consumers treat them the same.
        // Right must be non-null to invoke MergeCustomFormats and exercise normalization.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml { TrashIds = ["cf1"], AssignScoresTo = [] },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "P", Score = 10 }],
                },
            ],
        };

        var sut = new SonarrConfigMerger();
        var result = sut.Merge(leftConfig, rightConfig);

        var cf1Entry = result.CustomFormats?.SingleOrDefault(x =>
            x.TrashIds != null && x.TrashIds.Contains("cf1")
        );
        cf1Entry.Should().NotBeNull();
        cf1Entry.AssignScoresTo.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Profile_name_matching_is_case_insensitive()
    {
        // Profile key lookup uses InvariantCultureIgnoreCase; "Profile A" on A matches
        // "profile a" on B and its trash IDs are subtracted.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A", Score = 100 },
                    ],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "profile a", Score = 100 },
                    ],
                },
            ],
        };

        var sut = new SonarrConfigMerger();
        var result = sut.Merge(leftConfig, rightConfig);

        result
            .CustomFormats.Should()
            .ContainEquivalentOf(
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf2"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A", Score = 100 },
                    ],
                }
            );
    }

    [Test]
    public void Deduplication_ignores_score_mismatch_between_sides()
    {
        // Profile key matching uses name/trash_id only; score is ignored.
        // If A and B target the same profile with different scores, B's trash IDs are still
        // subtracted from A's entry.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "P", Score = 100 }],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "P", Score = 200 }],
                },
            ],
        };

        var sut = new SonarrConfigMerger();
        var result = sut.Merge(leftConfig, rightConfig);

        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "P", Score = 100 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf1"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { Name = "P", Score = 200 },
                            ],
                        },
                    ],
                }
            );
    }

    [Test]
    public void Profile_less_entries_on_both_sides_are_not_deduplicated()
    {
        // Entries without AssignScoresTo are "untargeted" and should not match each other;
        // both sides pass through independently even when trash IDs overlap.
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats = [new CustomFormatConfigYaml { TrashIds = ["cf1", "cf2"] }],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats = [new CustomFormatConfigYaml { TrashIds = ["cf1", "cf3"] }],
        };

        var sut = new SonarrConfigMerger();
        var result = sut.Merge(leftConfig, rightConfig);

        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml { TrashIds = ["cf1", "cf2"] },
                        new CustomFormatConfigYaml { TrashIds = ["cf1", "cf3"] },
                    ],
                }
            );
    }

    [Test]
    public void Profiles_matched_by_trash_id_deduplicate_correctly()
    {
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf2"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { TrashId = "profile-123", Score = 100 },
                    ],
                },
            ],
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1", "cf3"],
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { TrashId = "profile-123", Score = 200 },
                    ],
                },
            ],
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        // cf1 is in both, so it should be removed from left (right wins)
        // cf2 is only in left, so it should remain
        result
            .Should()
            .BeEquivalentTo(
                new SonarrConfigYaml
                {
                    CustomFormats =
                    [
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf2"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { TrashId = "profile-123", Score = 100 },
                            ],
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = ["cf1", "cf3"],
                            AssignScoresTo =
                            [
                                new QualityScoreConfigYaml { TrashId = "profile-123", Score = 200 },
                            ],
                        },
                    ],
                }
            );
    }
}
