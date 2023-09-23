using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TagConfigPhaseTest
{
    [Test, AutoMockData]
    public void Return_null_when_list_empty(TagConfigPhase sut)
    {
        var config = NewConfig.Sonarr() with
        {
            ReleaseProfiles = Array.Empty<ReleaseProfileConfig>()
        };

        var result = sut.Execute(config);
        result.Should().BeNull();
    }

    [Test, AutoMockData]
    public void Return_tags(TagConfigPhase sut)
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

        var result = sut.Execute(config);
        result.Should().BeEquivalentTo(config.ReleaseProfiles[0].Tags);
    }
}
