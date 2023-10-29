using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
public class TagApiFetchPhaseTest
{
    [Test, AutoMockData]
    public async Task Cache_is_updated(
        [Frozen] ISonarrTagApiService api,
        [Frozen] ServiceTagCache cache,
        TagPipelineContext context,
        TagApiFetchPhase sut)
    {
        var expectedData = new[]
        {
            new SonarrTag {Id = 3},
            new SonarrTag {Id = 4},
            new SonarrTag {Id = 5}
        };

        api.GetTags(default!).ReturnsForAnyArgs(expectedData);

        await sut.Execute(context, default!);
        cache.Tags.Should().BeEquivalentTo(expectedData);
    }
}
