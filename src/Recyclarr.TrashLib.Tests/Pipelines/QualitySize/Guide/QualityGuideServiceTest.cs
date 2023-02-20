using System.IO.Abstractions;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Pipelines.QualitySize.Guide;

namespace Recyclarr.TrashLib.Tests.Pipelines.QualitySize.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityGuideServiceTest : IntegrationFixture
{
    [TestCase(SupportedServices.Sonarr, "sonarr")]
    [TestCase(SupportedServices.Radarr, "radarr")]
    public void Get_data_for_service(SupportedServices service, string serviceDir)
    {
        Fs.AddFileFromEmbeddedResource(
            Paths.RepoDirectory.SubDir("docs", "json", serviceDir, "quality-size").File("metadata.json"),
            GetType(),
            "Data.quality_size.json");

        var sut = Resolve<QualityGuideService>();

        var result = sut.GetQualitySizeData(service);

        result.Should().BeEquivalentTo(new[]
        {
            new {TrashId = "bef99584217af744e404ed44a33af589"}
        });
    }
}
