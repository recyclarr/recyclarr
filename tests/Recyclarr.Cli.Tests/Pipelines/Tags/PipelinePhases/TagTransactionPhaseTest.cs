using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
public class TagTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Return_tags_in_config_that_do_not_exist_in_service(TagTransactionPhase sut)
    {
        var context = new TagPipelineContext
        {
            ConfigOutput = new[] {"one", "two", "three"},
            ApiFetchOutput = new[]
            {
                new SonarrTag {Label = "three"},
                new SonarrTag {Label = "four"}
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo("one", "two");
    }

    [Test, AutoMockData]
    public void Return_all_tags_if_none_exist(TagTransactionPhase sut)
    {
        var context = new TagPipelineContext
        {
            ConfigOutput = new[] {"one", "two", "three"},
            ApiFetchOutput = Array.Empty<SonarrTag>()
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo("one", "two", "three");
    }

    [Test, AutoMockData]
    public void No_tags_returned_if_all_exist(TagTransactionPhase sut)
    {
        var context = new TagPipelineContext
        {
            ConfigOutput = Array.Empty<string>(),
            ApiFetchOutput = new[]
            {
                new SonarrTag {Label = "three"},
                new SonarrTag {Label = "four"}
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEmpty();
    }
}
