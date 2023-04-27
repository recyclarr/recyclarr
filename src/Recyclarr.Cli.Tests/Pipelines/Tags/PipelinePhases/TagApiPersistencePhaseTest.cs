using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Pipelines.Tags.Api;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Pipelines.Tags.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TagApiPersistencePhaseTest
{
    [Test, AutoMockData]
    public async Task Persisted_tags_are_added_to_cache(
        [Frozen] ISonarrTagApiService api,
        [Frozen] ServiceTagCache cache,
        TagApiPersistencePhase sut)
    {
        cache.AddTags(new[]
        {
            new SonarrTag {Id = 1},
            new SonarrTag {Id = 2}
        });

        var config = Substitute.For<IServiceConfiguration>();
        var tagsToCreate = new[] {"three", "four"};

        api.CreateTag(config, "three").Returns(new SonarrTag {Id = 3});
        api.CreateTag(config, "four").Returns(new SonarrTag {Id = 4});

        await sut.Execute(config, tagsToCreate);

        cache.Tags.Should().BeEquivalentTo(new[]
        {
            new SonarrTag {Id = 1},
            new SonarrTag {Id = 2},
            new SonarrTag {Id = 3},
            new SonarrTag {Id = 4}
        });
    }
}
