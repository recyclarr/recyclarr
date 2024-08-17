using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class MergeQualityProfilesTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityProfiles =
            [
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = ["except1"]
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities =
                    [
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = ["quality"]
                        }
                    ]
                }
            ]
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
            QualityProfiles =
            [
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = ["except1"]
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities =
                    [
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = ["quality"]
                        }
                    ]
                }
            ]
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public void Non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityProfiles =
            [
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = ["except1"]
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities =
                    [
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = ["quality"]
                        }
                    ]
                }
            ]
        };

        var rightConfig = new SonarrConfigYaml
        {
            QualityProfiles =
            [
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    ScoreSet = "set2",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Except = ["except2", "except3"]
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        UntilQuality = "quality2"
                    },
                    Qualities =
                    [
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = false,
                            Name = "quality2",
                            Qualities = ["quality3"]
                        },
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality4",
                            Qualities = ["quality5", "quality6"]
                        }
                    ]
                }
            ]
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(new SonarrConfigYaml
        {
            QualityProfiles =
            [
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set2",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = ["except1", "except2", "except3"]
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality2",
                        UntilScore = 200
                    },
                    Qualities =
                    [
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = false,
                            Name = "quality2",
                            Qualities = ["quality3"]
                        },
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality4",
                            Qualities = ["quality5", "quality6"]
                        }
                    ]
                }
            ]
        });
    }
}
