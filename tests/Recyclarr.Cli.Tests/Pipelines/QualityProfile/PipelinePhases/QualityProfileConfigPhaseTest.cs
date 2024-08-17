using AutoFixture;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.Tests.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
public class QualityProfileConfigPhaseTest
{
    private static RadarrConfiguration SetupCfs(params CustomFormatConfig[] cfConfigs)
    {
        return NewConfig.Radarr() with
        {
            CustomFormats = cfConfigs
        };
    }

    [Test]
    public void All_cfs_use_score_override()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.DataWithScore("", "id1", 101, 1),
            NewCf.DataWithScore("", "id2", 201, 2)
        ]);

        fixture.Inject<IServiceConfiguration>(SetupCfs(new CustomFormatConfig
        {
            TrashIds = ["id1", "id2"],
            AssignScoresTo = new List<AssignScoresToConfig>
            {
                new()
                {
                    Name = "test_profile",
                    Score = 100
                }
            }
        }));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();

        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEquivalentTo([
                NewQp.Processed("test_profile", ("id1", 1, 100), ("id2", 2, 100))
            ],
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test]
    public void All_cfs_use_guide_scores_with_no_override()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.DataWithScore("", "id1", 100, 1),
            NewCf.DataWithScore("", "id2", 200, 2)
        ]);

        fixture.Inject<IServiceConfiguration>(SetupCfs(new CustomFormatConfig
        {
            TrashIds = ["id1", "id2"],
            AssignScoresTo = new List<AssignScoresToConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        }));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEquivalentTo([
                NewQp.Processed("test_profile", ("id1", 1, 100), ("id2", 2, 200))
            ],
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test]
    public void No_cfs_returned_when_no_score_in_guide_or_config()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.Data("", "id1", 1),
            NewCf.Data("", "id2", 2)
        ]);

        fixture.Inject<IServiceConfiguration>(SetupCfs(new CustomFormatConfig
        {
            TrashIds = ["id1", "id2"],
            AssignScoresTo = new List<AssignScoresToConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        }));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEquivalentTo([
                NewQp.Processed("test_profile")
            ],
            o => o.Excluding(x => x.ShouldCreate).Excluding(x => x.ScorelessCfs));
    }

    [Test]
    public void Skip_duplicate_cfs_with_same_and_different_scores()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.DataWithScore("", "id1", 100, 1)
        ]);

        fixture.Inject<IServiceConfiguration>(SetupCfs(
            new CustomFormatConfig
            {
                TrashIds = ["id1"]
            },
            new CustomFormatConfig
            {
                TrashIds = ["id1"],
                AssignScoresTo = new List<AssignScoresToConfig>
                {
                    new() {Name = "test_profile1", Score = 100}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = ["id1"],
                AssignScoresTo = new List<AssignScoresToConfig>
                {
                    new() {Name = "test_profile1", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = ["id1"],
                AssignScoresTo = new List<AssignScoresToConfig>
                {
                    new() {Name = "test_profile2", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = ["id1"],
                AssignScoresTo = new List<AssignScoresToConfig>
                {
                    new() {Name = "test_profile2", Score = 100}
                }
            }
        ));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEquivalentTo([
                NewQp.Processed("test_profile1", ("id1", 1, 100)),
                NewQp.Processed("test_profile2", ("id1", 1, 200))
            ],
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test]
    public void All_cfs_use_score_set()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.DataWithScores("", "id1", 1, ("default", 101), ("set1", 102)),
            NewCf.DataWithScores("", "id2", 2, ("default", 201), ("set2", 202))
        ]);

        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["id1", "id2"],
                    AssignScoresTo =
                    [
                        new AssignScoresToConfig {Name = "test_profile"}
                    ]
                }
            ],
            QualityProfiles =
            [
                new QualityProfileConfig
                {
                    Name = "test_profile",
                    ScoreSet = "set1"
                }
            ]
        };
        fixture.Inject<IServiceConfiguration>(config);

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEquivalentTo([
                NewQp.Processed("test_profile", ("id1", 1, 102), ("id2", 2, 201)) with
                {
                    Profile = config.QualityProfiles.First()
                }
            ],
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test]
    public void Empty_trash_ids_list_is_ignored()
    {
        var fixture = NSubstituteFixture.Create();

        fixture.Inject<IServiceConfiguration>(SetupCfs(new CustomFormatConfig
        {
            TrashIds = Array.Empty<string>(),
            AssignScoresTo = new List<AssignScoresToConfig>
            {
                new()
                {
                    Name = "test_profile",
                    Score = 100
                }
            }
        }));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEmpty();
    }

    [Test]
    public void Empty_quality_profiles_is_ignored()
    {
        var fixture = NSubstituteFixture.Create();

        var cache = fixture.Freeze<ProcessedCustomFormatCache>();
        cache.AddCustomFormats([
            NewCf.DataWithScore("", "id1", 101, 1),
            NewCf.DataWithScore("", "id2", 201, 2)
        ]);

        fixture.Inject<IServiceConfiguration>(SetupCfs(new CustomFormatConfig
        {
            TrashIds = ["id1", "id2"],
            AssignScoresTo = Array.Empty<AssignScoresToConfig>()
        }));

        var context = new QualityProfilePipelineContext();
        var sut = fixture.Create<QualityProfileConfigPhase>();
        sut.Execute(context, CancellationToken.None);

        context.ConfigOutput.Should().BeEmpty();
    }
}
