using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Cache;
using Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileConfigPhaseTest
{
    [Test, AutoMockData]
    public void Reset_unmatched_scores_promoted_to_quality_profiles_property_when_no_quality_profiles_in_config(
        QualityProfileConfigPhase sut)
    {
        var config = new RadarrConfiguration
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "test_profile",
                            ResetUnmatchedScores = true
                        }
                    }
                }
            }
        };

        sut.Execute(config);

        config.QualityProfiles.Should().BeEquivalentTo(new QualityProfileConfig[]
        {
            new() {Name = "test_profile", ResetUnmatchedScores = true}
        });
    }

    [Test, AutoMockData]
    public void Reset_unmatched_scores_promoted_to_quality_profiles_property_when_quality_profile_in_config(
        QualityProfileConfigPhase sut)
    {
        var config = new RadarrConfiguration
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfig
                {
                    Name = "test_profile",
                    ResetUnmatchedScores = null
                }
            },
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "test_profile",
                            ResetUnmatchedScores = true
                        }
                    }
                }
            }
        };

        sut.Execute(config);

        config.QualityProfiles.Should().BeEquivalentTo(new QualityProfileConfig[]
        {
            new() {Name = "test_profile", ResetUnmatchedScores = true}
        });
    }

    [Test, AutoMockData]
    public void Reset_unmatched_scores_not_promoted_to_quality_profiles_property_when_false(
        QualityProfileConfigPhase sut)
    {
        var config = new RadarrConfiguration
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "test_profile",
                            ResetUnmatchedScores = false
                        }
                    }
                }
            }
        };

        sut.Execute(config);

        config.QualityProfiles.Should().BeEmpty();
    }

    private static RadarrConfiguration SetupCfs(params CustomFormatConfig[] cfConfigs)
    {
        return new RadarrConfiguration
        {
            CustomFormats = cfConfigs
        };
    }

    [Test, AutoMockData]
    public void All_cfs_use_score_override(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 101, 1),
            NewCf.DataWithScore("", "id2", 201, 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile",
                    Score = 100
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
        {
            NewQp.Processed("test_profile", (1, 100), (2, 100))
        });
    }

    [Test, AutoMockData]
    public void All_cfs_use_guide_scores_with_no_override(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 100, 1),
            NewCf.DataWithScore("", "id2", 200, 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
        {
            NewQp.Processed("test_profile", (1, 100), (2, 200))
        });
    }

    [Test, AutoMockData]
    public void No_cfs_returned_when_no_score_in_guide_or_config(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.Data("", "id1", 1),
            NewCf.Data("", "id2", 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Skip_duplicate_cfs_with_same_and_different_scores(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 100, 1)
        });

        var config = SetupCfs(
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"}
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile1", Score = 100}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile1", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile2", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile2", Score = 100}
                }
            }
        );

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
        {
            NewQp.Processed("test_profile1", (1, 100)),
            NewQp.Processed("test_profile2", (1, 200))
        });
    }

    [Test, AutoMockData]
    public void Use_existing_config_quality_profile_when_specified(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 100, 1)
        });

        var config = new RadarrConfiguration
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfig
                {
                    Name = "test_profile",
                    ResetUnmatchedScores = true
                }
            },
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new[] {"id1"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "test_profile",
                            ResetUnmatchedScores = false // Should be ignored because top-level QP has it set already
                        }
                    }
                }
            }
        };

        sut.Execute(config);

        config.QualityProfiles.Should().BeEquivalentTo(new QualityProfileConfig[]
        {
            new() {Name = "test_profile", ResetUnmatchedScores = true}
        });
    }
}
