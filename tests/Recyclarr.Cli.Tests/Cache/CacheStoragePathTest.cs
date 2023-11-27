using Recyclarr.Cli.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Tests.Cache;

[TestFixture]
public class CacheStoragePathTest
{
    [Test, AutoMockData]
    public void Use_correct_name_in_path(CacheStoragePath sut)
    {
        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something/foo/bar"),
            InstanceName = "thename"
        };

        var result = sut.CalculatePath(config, "obj");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]obj\.json$");
    }
}
