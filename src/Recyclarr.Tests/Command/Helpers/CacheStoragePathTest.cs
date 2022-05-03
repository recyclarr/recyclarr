using Autofac;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command.Helpers;
using Recyclarr.TestLibrary;
using TrashLib.Config.Services;

namespace Recyclarr.Tests.Command.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CacheStoragePathTest : IntegrationFixture
{
    [Test]
    public void Use_guid_when_empty_name()
    {
        var config = Substitute.ForPartsOf<ServiceConfiguration>();
        config.BaseUrl = "something";
        config.Name = "";

        using var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(config).AsImplementedInterfaces();
        });

        var sut = scope.Resolve<CacheStoragePath>();
        var result = sut.CalculatePath("obj");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]obj\.json$");
    }

    [Test]
    public void Use_name_when_not_empty()
    {
        var config = Substitute.ForPartsOf<ServiceConfiguration>();
        config.BaseUrl = "something";
        config.Name = "thename";

        using var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(config).AsImplementedInterfaces();
        });

        var sut = scope.Resolve<CacheStoragePath>();
        var result = sut.CalculatePath("obj");

        result.FullName.Should().MatchRegex(@".*[/\\]thename[/\\]obj\.json$");
    }
}
