using Recyclarr.Cli.Pipelines.Tags.Api;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TagTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Return_tags_in_config_that_do_not_exist_in_service(TagTransactionPhase sut)
    {
        var configTags = new[] {"one", "two", "three"};
        var serviceTags = new[]
        {
            new SonarrTag {Label = "three"},
            new SonarrTag {Label = "four"}
        };

        var result = sut.Execute(configTags, serviceTags);

        result.Should().BeEquivalentTo("one", "two");
    }

    [Test, AutoMockData]
    public void Return_all_tags_if_none_exist(TagTransactionPhase sut)
    {
        var configTags = new[] {"one", "two", "three"};
        var serviceTags = Array.Empty<SonarrTag>();

        var result = sut.Execute(configTags, serviceTags);

        result.Should().BeEquivalentTo("one", "two", "three");
    }

    [Test, AutoMockData]
    public void No_tags_returned_if_all_exist(TagTransactionPhase sut)
    {
        var configTags = Array.Empty<string>();
        var serviceTags = new[]
        {
            new SonarrTag {Label = "three"},
            new SonarrTag {Label = "four"}
        };

        var result = sut.Execute(configTags, serviceTags);

        result.Should().BeEmpty();
    }
}
