using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MergeCustomFormatsTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
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
    public void Non_empty_right_to_empty_left()
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
    public void Non_empty_right_to_non_empty_left()
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
                        new QualityScoreConfigYaml {Name = "c", Score = 100},
                        new QualityScoreConfigYaml {Name = "d", Score = 101},
                        new QualityScoreConfigYaml {Name = "e", Score = 102}
                    }
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "f", Score = 100}
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
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 50}
                    }
                }
            }
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(new SonarrConfigYaml
        {
            CustomFormats = new[]
            {
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 100}
                    }
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "d", Score = 101}
                    }
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "e", Score = 102}
                    }
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id2"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "f", Score = 100}
                    }
                },
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
                },
                new CustomFormatConfigYaml
                {
                    TrashIds = new[] {"id1"},
                    QualityProfiles = new[]
                    {
                        new QualityScoreConfigYaml {Name = "c", Score = 50}
                    }
                }
            }
        });
    }
}
