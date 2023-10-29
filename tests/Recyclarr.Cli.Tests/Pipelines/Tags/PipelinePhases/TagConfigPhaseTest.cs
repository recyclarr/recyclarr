using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.Tests.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
public class TagConfigPhaseTest
{
    [Test, AutoMockData]
    public async Task Output_empty_when_config_has_no_tags(TagConfigPhase sut)
    {
        var context = new TagPipelineContext();
        var config = NewConfig.Sonarr() with
        {
            ReleaseProfiles = Array.Empty<ReleaseProfileConfig>()
        };

        await sut.Execute(context, config);
        context.ConfigOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Output_not_empty_when_config_has_tags(TagConfigPhase sut)
    {
        var config = NewConfig.Sonarr() with
        {
            ReleaseProfiles = new[]
            {
                new ReleaseProfileConfig
                {
                    Tags = new[] {"one", "two", "three"}
                }
            }
        };

        var context = new TagPipelineContext();
        sut.Execute(context, config);
        context.ConfigOutput.Should().BeEquivalentTo(config.ReleaseProfiles[0].Tags);
    }
}
