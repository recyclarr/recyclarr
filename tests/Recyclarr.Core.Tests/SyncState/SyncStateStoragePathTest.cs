using AutoFixture;
using Recyclarr.Config.Models;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

internal sealed class SyncStateStoragePathTest
{
    [Test]
    public void Use_correct_name_in_path()
    {
        var fixture = NSubstituteFixture.Create();

        fixture.Inject<IServiceConfiguration>(
            new SonarrConfiguration
            {
                BaseUrl = new Uri("http://something/foo/bar"),
                InstanceName = "thename",
            }
        );

        var sut = fixture.Create<SyncStateStoragePath>();
        var result = sut.CalculatePath("azAZ_09");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]azAZ_09\.json$");
    }

    [Test]
    public void Loading_with_invalid_object_name_throws()
    {
        var fixture = NSubstituteFixture.Create();
        var sut = fixture.Create<SyncStateStoragePath>();

        Action act = () => sut.CalculatePath("invalid+name");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }
}
