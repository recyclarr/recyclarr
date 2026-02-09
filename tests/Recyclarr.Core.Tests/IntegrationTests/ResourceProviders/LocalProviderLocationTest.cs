using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

[CoreDataSource]
internal sealed class LocalProviderLocationTest(
    LocalProviderLocation.Factory factory,
    MockFileSystem fs,
    IAppPaths paths
)
{
    [Test]
    public async Task Resolve_relative_path_against_app_data_directory()
    {
        var expectedDir = paths.ConfigDirectory.SubDirectory("my-custom-formats");
        fs.AddDirectory(expectedDir.FullName);

        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = "my-custom-formats",
            Service = "radarr",
        };

        var sut = factory(config);

        var result = await sut.InitializeAsync(null, CancellationToken.None);

        result.Should().ContainSingle().Which.FullName.Should().Be(expectedDir.FullName);
    }

    [Test]
    public async Task Absolute_path_used_directly()
    {
        var absoluteDir = fs.CurrentDirectory().SubDirectory("absolute").SubDirectory("path");
        fs.AddDirectory(absoluteDir.FullName);

        var config = new LocalResourceProvider
        {
            Name = "test",
            Type = "custom-formats",
            Path = absoluteDir.FullName,
            Service = "radarr",
        };

        var sut = factory(config);

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

        var sut = factory(config);

        var result = await sut.InitializeAsync(null, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
