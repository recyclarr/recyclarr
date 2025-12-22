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
