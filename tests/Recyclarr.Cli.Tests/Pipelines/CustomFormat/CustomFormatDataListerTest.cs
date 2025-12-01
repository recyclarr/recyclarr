using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        IListCustomFormatSettings settings,
        IConsoleOutputSettings outputSettings
    )
    {
        var radarrData = new[]
        {
            NewCf.RadarrData("First", "123"),
            NewCf.RadarrData("Second", "456"),
        };
        var sonarrData = new[]
        {
            NewCf.SonarrData("First", "123"),
            NewCf.SonarrData("Second", "456"),
        };

        var fs = new MockFileSystem();
        using var console = new TestConsole();

        fs.AddFile("/radarr/cf1.json", new MockFileData(SerializeResource(radarrData[0])));
        fs.AddFile("/radarr/cf2.json", new MockFileData(SerializeResource(radarrData[1])));
        fs.AddFile("/sonarr/cf1.json", new MockFileData(SerializeResource(sonarrData[0])));
        fs.AddFile("/sonarr/cf2.json", new MockFileData(SerializeResource(sonarrData[1])));

        var registry = new ResourceRegistry<IFileInfo>();
        registry.Register<RadarrCustomFormatResource>([
            fs.FileInfo.New("/radarr/cf1.json"),
            fs.FileInfo.New("/radarr/cf2.json"),
        ]);
        registry.Register<SonarrCustomFormatResource>([
            fs.FileInfo.New("/sonarr/cf1.json"),
            fs.FileInfo.New("/sonarr/cf2.json"),
        ]);

        var loader = new JsonResourceLoader();

        var categoryRegistry = new ResourceRegistry<IFileInfo>();
        categoryRegistry.Register<RadarrCategoryMarkdownResource>([]);
        categoryRegistry.Register<SonarrCategoryMarkdownResource>([]);
        var categoryParser = Substitute.For<ICustomFormatCategoryParser>();
        var log = Substitute.For<ILogger>();
        var categoryQuery = new CategoryResourceQuery(categoryRegistry, categoryParser, log);

        var guide = new CustomFormatResourceQuery(registry, loader, categoryQuery, log);
        var sut = new CustomFormatDataLister(console, guide);

        settings.ScoreSets.Returns(false);
        settings.Service.Returns(SupportedServices.Radarr);

        sut.List(outputSettings, settings);

        console.Output.Should().ContainAll(radarrData.SelectMany(x => new[] { x.Name, x.TrashId }));
    }

    private static string SerializeResource(CustomFormatResource resource)
    {
        return JsonSerializer.Serialize(resource, GlobalJsonSerializerSettings.Guide);
    }
}
