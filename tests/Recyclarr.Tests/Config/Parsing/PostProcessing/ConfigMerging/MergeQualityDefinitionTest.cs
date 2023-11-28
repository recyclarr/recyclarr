using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class MergeQualityDefinitionTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
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
    public void Non_empty_right_to_empty_left()
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
    public void Non_empty_right_to_non_empty_left()
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
}
