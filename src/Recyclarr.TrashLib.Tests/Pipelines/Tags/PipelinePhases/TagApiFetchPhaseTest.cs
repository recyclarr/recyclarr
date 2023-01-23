using Recyclarr.TrashLib.Pipelines.Tags;
using Recyclarr.TrashLib.Pipelines.Tags.Api;
using Recyclarr.TrashLib.Pipelines.Tags.PipelinePhases;

namespace Recyclarr.TrashLib.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TagApiFetchPhaseTest
{
    [Test, AutoMockData]
    public async Task Cache_is_cleared_and_updated(
        [Frozen] ISonarrTagApiService api,
        [Frozen] ServiceTagCache cache,
        TagApiFetchPhase sut)
    {
        var expectedData = new[]
        {
            new SonarrTag {Id = 3},
            new SonarrTag {Id = 4},
            new SonarrTag {Id = 5}
        };

        api.GetTags(default!).ReturnsForAnyArgs(expectedData);

        cache.AddTags(new[]
        {
            new SonarrTag {Id = 1},
            new SonarrTag {Id = 2}
        });

        await sut.Execute(default!);
        cache.Tags.Should().BeEquivalentTo(expectedData);
    }
}
