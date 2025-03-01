using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class MergeApiKeyTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml { ApiKey = "a" };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        // API Key should not be merged!
        var rightConfig = new SonarrConfigYaml { ApiKey = "b" };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml { ApiKey = "a" };

        // API Key should not be merged!
        var rightConfig = new SonarrConfigYaml { ApiKey = "b" };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }
}
