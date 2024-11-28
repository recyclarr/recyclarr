using System.IO.Abstractions;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.IntegrationTests;

[TestFixture]
public class QualitySizeGuideServiceTest : IntegrationTestFixture
{
    [TestCase(SupportedServices.Sonarr, "sonarr")]
    [TestCase(SupportedServices.Radarr, "radarr")]
    public void Get_data_for_service(SupportedServices service, string serviceDir)
    {
        var repo = Resolve<ITrashGuidesRepo>();
        const string metadataJson = """
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
            repo.Path.SubDirectory("docs", "json", serviceDir, "quality-size")
                .File("some-quality-size.json"),
            GetType(),
            "Data.quality_size.json"
        );

        var sut = Resolve<QualitySizeGuideService>();

        var result = sut.GetQualitySizeData(service);

        result.Should().ContainSingle(x => x.Type == "series");
    }
}
