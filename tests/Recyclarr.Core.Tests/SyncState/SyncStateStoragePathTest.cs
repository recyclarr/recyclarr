using System.IO.Abstractions;
using Recyclarr.Config.Models;
using Recyclarr.Platform;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

internal sealed class SyncStateStoragePathTest
{
    private static SyncStateStoragePath CreateSut()
    {
        var fs = new MockFileSystem();
        var paths = Substitute.For<IAppPaths>();
        paths.StateDirectory.Returns(fs.CurrentDirectory().SubDirectory("state"));

        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something/foo/bar"),
            InstanceName = "thename",
        };

        return new SyncStateStoragePath(paths, config);
    }

    [Test]
    public void Use_correct_name_in_path()
    {
        var sut = CreateSut();
        var result = sut.CalculatePath("azAZ_09");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]azAZ_09\.json$");
    }

    [Test]
    public void Loading_with_invalid_object_name_throws()
    {
        var sut = CreateSut();

        Action act = () => sut.CalculatePath("invalid+name");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }
}
