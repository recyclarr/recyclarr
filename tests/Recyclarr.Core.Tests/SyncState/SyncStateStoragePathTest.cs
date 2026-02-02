using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using Recyclarr.Config.Models;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

[SuppressMessage(
    "ReSharper",
    "ClassNeverInstantiated.Local",
    Justification = "POCO objects for testing"
)]
[SuppressMessage(
    "Performance",
    "CA1812: Avoid uninstantiated internal classes",
    Justification = "For testing only"
)]
[SuppressMessage("SonarLint", "S2094", Justification = "Used for unit test scenario")]
internal sealed class SyncStateStoragePathTest
{
    private const string ValidObjectName = "azAZ_09";

    private sealed class ObjectWithoutAttribute;

    [SyncStateName(ValidObjectName)]
    private sealed class ObjectWithAttribute;

    [SyncStateName("invalid+name")]
    private sealed class ObjectWithAttributeInvalidChars;

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
        var result = sut.CalculatePath<ObjectWithAttribute>();

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]azAZ_09\.json$");
    }

    [Test]
    public void Loading_with_invalid_object_name_throws()
    {
        var fixture = NSubstituteFixture.Create();
        var sut = fixture.Create<SyncStateStoragePath>();

        Action act = () => sut.CalculatePath<ObjectWithAttributeInvalidChars>();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }

    [Test]
    public void Loading_without_attribute_throws()
    {
        var fixture = NSubstituteFixture.Create();
        var sut = fixture.Create<SyncStateStoragePath>();

        Action act = () => sut.CalculatePath<ObjectWithoutAttribute>();

        act.Should().Throw<ArgumentException>().WithMessage("SyncStateNameAttribute is missing*");
    }
}
