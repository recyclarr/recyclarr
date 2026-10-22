using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class LocalProviderLocationTest : CoreIntegrationTestFixture
{
    [Test]
    public void Resolve_relative_path_against_app_data_directory()
    {
        var expectedDir = Paths.ConfigDirectory.SubDirectory("my-custom-formats");
        Fs.AddDirectory(expectedDir.FullName);

        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = "my-custom-formats",
            Service = "radarr",
        };

        var sut = CreateSut(config);

        sut.Roots.Should().ContainSingle().Which.FullName.Should().Be(expectedDir.FullName);
    }

    [Test]
    public void Absolute_path_used_directly()
    {
        var absoluteDir = Fs.CurrentDirectory().SubDirectory("absolute").SubDirectory("path");
        Fs.AddDirectory(absoluteDir.FullName);

        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = absoluteDir.FullName,
            Service = "radarr",
        };

        var sut = CreateSut(config);

        sut.Roots.Should().ContainSingle().Which.FullName.Should().Be(absoluteDir.FullName);
    }

    [Test]
    public void Nonexistent_relative_path_returns_empty_collection()
    {
        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = "nonexistent",
            Service = "radarr",
        };

        var sut = CreateSut(config);

        sut.Roots.Should().BeEmpty();
    }

    private LocalProviderLocation CreateSut(LocalResourceProvider config) =>
        Resolve<LocalProviderLocation.Factory>()(config, Resolve<CustomFormatsStrategy>());
}
