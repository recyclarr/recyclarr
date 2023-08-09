using System.IO.Abstractions;
using Recyclarr.Cli.Pipelines.QualitySize.Guide;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityGuideServiceTest : CliIntegrationFixture
{
    [TestCase(SupportedServices.Sonarr, "sonarr")]
    [TestCase(SupportedServices.Radarr, "radarr")]
    public void Get_data_for_service(SupportedServices service, string serviceDir)
    {
        var repo = Resolve<ITrashGuidesRepo>();
        const string metadataJson =
            """
            {
              "json_paths": {
                "radarr": {
                  "qualities": ["docs/json/radarr/quality-size"]
                },
                "sonarr": {
                  "qualities": ["docs/json/sonarr/quality-size"]
                }
              }
            }
            """;

        Fs.AddFile(repo.Path.File("metadata.json"), new MockFileData(metadataJson));

        Fs.AddFileFromEmbeddedResource(
            repo.Path.SubDir("docs", "json", serviceDir, "quality-size").File("some-quality-size.json"),
            GetType(),
            "Data.quality_size.json");

        var sut = Resolve<QualityGuideService>();

        var result = sut.GetQualitySizeData(service);

        result.Should().ContainSingle(x => x.Type == "series");
    }
}
