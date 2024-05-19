using AutoFixture;
using Recyclarr.Cli.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Tests.Cache;

[TestFixture]
public class CacheStoragePathTest
{
    [Test]
    public void Use_correct_name_in_path()
    {
        var fixture = NSubstituteFixture.Create();

        fixture.Inject<IServiceConfiguration>(new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something/foo/bar"),
            InstanceName = "thename"
        });

        var sut = fixture.Create<CacheStoragePath>();
        var result = sut.CalculatePath("obj");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]obj\.json$");
    }
}
