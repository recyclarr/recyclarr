using System.Diagnostics.CodeAnalysis;
using FluentAssertions.Execution;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.TrashLib.Config.Tests.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceConfigMergerTest
{
    [Test]
    public void Merge_api_key_from_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            ApiKey = "a"
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_api_key_from_non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        // API Key should not be merged!
        var rightConfig = new SonarrConfigYaml
        {
            ApiKey = "b"
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_api_key_from_non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            ApiKey = "a"
        };

        // API Key should not be merged!
        var rightConfig = new SonarrConfigYaml
        {
            ApiKey = "b"
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    //------------------------------------------------------

    [Test]
    public void Merge_base_url_from_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            BaseUrl = "a"
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_base_url_from_non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        // BaseUrl should not be merged!
        var rightConfig = new SonarrConfigYaml
        {
            BaseUrl = "b"
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_base_url_from_non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            BaseUrl = "a"
        };

        // Baseurl should not be merged!
        var rightConfig = new SonarrConfigYaml
        {
            BaseUrl = "b"
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    //------------------------------------------------------

    [Test]
    public void Merge_quality_definition_from_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "type1",
                PreferredRatio = 0.5m
            }
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_quality_definition_from_non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        var rightConfig = new SonarrConfigYaml
        {
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "type1",
                PreferredRatio = 0.5m
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    public void Merge_quality_definition_from_non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "type1",
                PreferredRatio = 0.5m
            }
        };

        var rightConfig = new SonarrConfigYaml
        {
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "type2",
                PreferredRatio = 1.0m
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    //------------------------------------------------------

    [Test]
    public void Merge_custom_formats_from_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats = new[]
            {
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 100}
                    }
                }
            }
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_custom_formats_from_non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats = new[]
            {
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 100}
                    }
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    public void Merge_custom_formats_from_non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            CustomFormats = new[]
            {
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 100}
                    }
                }
            }
        };

        var rightConfig = new SonarrConfigYaml
        {
            CustomFormats = new[]
            {
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id3", "id4"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "d", Score = 200}
                    }
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id5", "id6"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "e", Score = 300}
                    }
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(new SonarrConfigYaml
        {
            CustomFormats = leftConfig.CustomFormats.Concat(rightConfig.CustomFormats).ToList()
        });
    }

    //------------------------------------------------------

    [Test]
    public void Merge_quality_profiles_from_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = new[] {"except1"}
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = new[] {"quality"}
                        }
                    }
                }
            }
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Merge_quality_profiles_from_non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        var rightConfig = new SonarrConfigYaml
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = new[] {"except1"}
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = new[] {"quality"}
                        }
                    }
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public void Merge_quality_profiles_from_non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = new[] {"except1"}
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality1",
                        UntilScore = 200
                    },
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality1",
                            Qualities = new[] {"quality"}
                        }
                    }
                }
            }
        };

        var rightConfig = new SonarrConfigYaml
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    ScoreSet = "set2",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Except = new[] {"except2", "except3"}
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        UntilQuality = "quality2"
                    },
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = false,
                            Name = "quality2",
                            Qualities = new[] {"quality3"}
                        },
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality4",
                            Qualities = new[] {"quality5", "quality6"}
                        }
                    }
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        using var scope = new AssertionScope().UsingLineBreaks;

        result.Should().BeEquivalentTo(new SonarrConfigYaml
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfigYaml
                {
                    Name = "e",
                    QualitySort = QualitySortAlgorithm.Top,
                    MinFormatScore = 100,
                    ScoreSet = "set2",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfigYaml
                    {
                        Enabled = true,
                        Except = new[] {"except1", "except2", "except3"}
                    },
                    Upgrade = new QualityProfileFormatUpgradeYaml
                    {
                        Allowed = true,
                        UntilQuality = "quality2",
                        UntilScore = 200
                    },
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = false,
                            Name = "quality2",
                            Qualities = new[] {"quality3"}
                        },
                        new QualityProfileQualityConfigYaml
                        {
                            Enabled = true,
                            Name = "quality4",
                            Qualities = new[] {"quality5", "quality6"}
                        }
                    }
                }
            }
        });
    }
}
