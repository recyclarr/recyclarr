using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class LocalProviderLocationTest : IntegrationTestFixture
{
    [Test]
    public async Task Resolve_relative_path_against_app_data_directory()
    {
        var expectedDir = Paths.AppDataDirectory.SubDirectory("my-custom-formats");
        Fs.AddDirectory(expectedDir.FullName);

        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = "my-custom-formats",
            Service = "radarr",
        };

        var sut = Resolve<LocalProviderLocation.Factory>()(config);

        var result = await sut.InitializeAsync(null, CancellationToken.None);

        result.Should().ContainSingle().Which.FullName.Should().Be(expectedDir.FullName);
    }

    [Test]
    public async Task Absolute_path_used_directly()
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

        var sut = Resolve<LocalProviderLocation.Factory>()(config);

        var result = await sut.InitializeAsync(null, CancellationToken.None);

        result.Should().ContainSingle().Which.FullName.Should().Be(absoluteDir.FullName);
    }

    [Test]
    public async Task Nonexistent_relative_path_returns_empty_collection()
    {
        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = "nonexistent",
            Service = "radarr",
        };

        var sut = Resolve<LocalProviderLocation.Factory>()(config);

        var result = await sut.InitializeAsync(null, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
