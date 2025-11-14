using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] CustomFormatResourceQuery guide,
        IListCustomFormatSettings settings,
        IConsoleOutputSettings outputSettings,
        CustomFormatDataLister sut
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

        guide.GetRadarr().Returns(radarrData);
        guide.GetSonarr().Returns(sonarrData);
        settings.ScoreSets.Returns(false);
        settings.Service.Returns(SupportedServices.Radarr);

        sut.List(outputSettings, settings);

        console.Output.Should().ContainAll(radarrData.SelectMany(x => new[] { x.Name, x.TrashId }));
    }
}
