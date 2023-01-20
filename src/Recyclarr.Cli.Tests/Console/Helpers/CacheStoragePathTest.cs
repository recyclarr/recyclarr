using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Console.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CacheStoragePathTest
{
    [Test, AutoMockData]
    public void Use_guid_when_no_name(CacheStoragePath sut)
    {
        var config = Substitute.ForPartsOf<ServiceConfiguration>();
        config.BaseUrl = new Uri("http://something");
        config.InstanceName = null;

        var result = sut.CalculatePath(config, "obj");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]obj\.json$");
    }

    [Test, AutoMockData]
    public void Use_name_when_not_null(CacheStoragePath sut)
    {
        var config = Substitute.ForPartsOf<ServiceConfiguration>();
        config.BaseUrl = new Uri("http://something");
        config.InstanceName = "thename";

        var result = sut.CalculatePath(config, "obj");

        result.FullName.Should().MatchRegex(@".*[/\\]thename_[a-f0-9]+[/\\]obj\.json$");
    }
}
