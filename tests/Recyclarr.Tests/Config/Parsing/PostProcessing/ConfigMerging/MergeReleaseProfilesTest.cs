using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MergeReleaseProfilesTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            ReleaseProfiles = new[]
            {
                new ReleaseProfileConfigYaml
                {
                    TrashIds = new[] {"id1"},
                    Filter = new ReleaseProfileFilterConfigYaml
                    {
                        Exclude = new[] {"exclude"},
                        Include = new[] {"include"}
                    },
                    Tags = new[] {"tag1", "tag2"},
                    StrictNegativeScores = true
                }
            }
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
            ReleaseProfiles = new[]
            {
                new ReleaseProfileConfigYaml
                {
                    TrashIds = new[] {"id1"},
                    Filter = new ReleaseProfileFilterConfigYaml
                    {
                        Exclude = new[] {"exclude"},
                        Include = new[] {"include"}
                    },
                    Tags = new[] {"tag1", "tag2"},
                    StrictNegativeScores = true
                }
            }
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
            ReleaseProfiles = new[]
            {
                new ReleaseProfileConfigYaml
                {
                    TrashIds = new[] {"id1"},
                    Filter = new ReleaseProfileFilterConfigYaml
                    {
                        Exclude = new[] {"exclude1"},
                        Include = new[] {"include1"}
                    },
                    Tags = new[] {"tag1", "tag2"},
                    StrictNegativeScores = true
                },
                new ReleaseProfileConfigYaml
                {
                    TrashIds = new[] {"id2", "id3"},
                    Filter = new ReleaseProfileFilterConfigYaml
                    {
                        Exclude = new[] {"exclude2"},
                        Include = new[] {"include2"}
                    },
                    Tags = new[] {"tag3"},
                    StrictNegativeScores = true
                }
            }
        };

        var rightConfig = new SonarrConfigYaml
        {
            ReleaseProfiles = new[]
            {
                new ReleaseProfileConfigYaml
                {
                    TrashIds = new[] {"id4"},
                    Filter = new ReleaseProfileFilterConfigYaml
                    {
                        Exclude = new[] {"exclude3"},
                        Include = new[] {"include3"}
                    },
                    Tags = new[] {"tag4", "tag5"},
                    StrictNegativeScores = false
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(new SonarrConfigYaml
        {
            ReleaseProfiles = leftConfig.ReleaseProfiles.Concat(rightConfig.ReleaseProfiles).ToList()
        });
    }
}
